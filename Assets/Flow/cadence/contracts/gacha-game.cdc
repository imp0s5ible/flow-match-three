 import GachaAvatars from "gacha-avatars.cdc" 
 import GachaGems from "gacha-gems.cdc"

 pub contract GachaGame {
    
    pub let AccountStoragePath : StoragePath
    pub let AccountPublicPath : PublicPath

    pub let GameProviderStoragePath : StoragePath
    pub let GameProviderPublicPath : PublicPath

    pub resource interface ReadAccount {
      pub fun get_name() : String
      pub fun borrow_avatar(uuid: UInt64) : &GachaAvatars.NFT?
      pub fun is_avatar_valid(uuid: UInt64) : Bool
      {
         return self.borrow_avatar(uuid: uuid) != nil
      }
    }

    pub resource interface CreditAccountAvatars {
      access(contract) fun add_avatar(avatar : @GachaAvatars.NFT)
    }

    pub resource interface BuyAccountAvatars {
      access(contract) fun buy_avatar_for(uuid: UInt64, payment : @GachaGems.Vault, for_account: Capability<&GachaGame.Account{CreditAccountAvatars}>)
      pub fun get_avatar_price(uuid: UInt64) : UFix64?
    }

    pub resource interface ConfigureAccount {
      pub fun set_name(new_name : String)
      pub fun set_avatar(uuid : UInt64)
      pub fun sell_avatar(uuid : UInt64, sell_price : UFix64)
    }

    pub resource interface AwardAccount {
      access(contract) fun award_gems(award : @GachaGems.Vault)
    }

    pub resource Account : ReadAccount, ConfigureAccount, AwardAccount, CreditAccountAvatars, BuyAccountAvatars {
      pub var name : String
      pub var authorizedAccount : PublicAccount
      pub var active_avatar_uuid : UInt64
      pub var gems : @GachaGems.Vault
      pub var owned_avatars : @{UInt64: GachaAvatars.NFT}
      pub var sell_prices : {UInt64: UFix64}
      pub var sold_avatars : @{UInt64: GachaAvatars.NFT}

      init(name : String, authorizedAccount: PublicAccount)
      {
         self.name = name
         self.authorizedAccount = authorizedAccount
         self.active_avatar_uuid = 0
         self.sell_prices = {}
         self.gems <- GachaGems.createEmptyVault()
         self.owned_avatars <- {}
         self.sold_avatars <- {}
      }

      pub fun is_owned_by_authorized_account(): Bool {
         return self.authorizedAccount.address == self.owner!.address
      }

      pub fun get_name() : String {
         return self.name
      }

      pub fun borrow_avatar(uuid: UInt64) : &GachaAvatars.NFT? {
         assert(self.is_owned_by_authorized_account())
         return &self.owned_avatars[uuid] as &GachaAvatars.NFT?
      }

      pub fun set_name(new_name : String) {
         assert(self.is_owned_by_authorized_account())
         self.name = new_name
      }

      pub fun set_avatar(uuid : UInt64)
      {
         assert(self.is_owned_by_authorized_account())
         if (uuid != 0) {
            assert(self.owned_avatars.containsKey(uuid))
         }
         self.active_avatar_uuid = uuid
      }

      pub fun buy_avatar_from(uuid: UInt64, from_account: Capability<&GachaGame.Account{BuyAccountAvatars}>, for_account: Capability<&GachaGame.Account{CreditAccountAvatars}>)
      {
         assert(self.is_owned_by_authorized_account())
         assert(for_account.borrow()!.uuid == self.uuid)

         let payment <- self.gems.withdraw(amount: from_account.borrow()!.get_avatar_price(uuid: uuid)!) as! @GachaGems.Vault
         from_account.borrow()!.buy_avatar_for(uuid: uuid, payment: <-payment, for_account: for_account)
      }

      access(contract) fun buy_avatar_for(uuid: UInt64, payment : @GachaGems.Vault, for_account: Capability<&GachaGame.Account{CreditAccountAvatars}>) {
         assert(self.is_owned_by_authorized_account())
         assert(payment.balance >= self.sell_prices[uuid]!, message: "not enough gems to buy this avatar!")
         self.gems.deposit(from: <-payment)
         var boughtAvatar: @GachaAvatars.NFT <- self.sold_avatars.remove(key: uuid)!
         for_account.borrow()!.add_avatar(avatar: <-boughtAvatar)
      }

      pub fun get_avatar_price(uuid: UInt64) : UFix64? {
         assert(self.is_owned_by_authorized_account())
         return self.sell_prices[uuid]
      }

      pub fun sell_avatar(uuid : UInt64, sell_price : UFix64) {
         assert(self.is_owned_by_authorized_account())
         let avatar_to_sell <- self.owned_avatars.remove(key:uuid)!
         assert(avatar_to_sell.minimumResalePrice <= sell_price);
         let sell_uuid = avatar_to_sell.uuid

         self.sold_avatars[sell_uuid] <-! avatar_to_sell
      }

      pub fun remove_avatar_from_sale(uuid: UInt64) {
         assert(self.is_owned_by_authorized_account())
         self.owned_avatars[uuid] <-! self.sold_avatars.remove(key: uuid)
      }

      access(contract) fun add_avatar(avatar : @GachaAvatars.NFT){
         assert(self.is_owned_by_authorized_account())
         self.owned_avatars[avatar.uuid] <-! avatar
      }

      access(contract) fun award_gems(award : @GachaGems.Vault) {
         assert(self.is_owned_by_authorized_account())
         self.gems.deposit(from: <-award)
      }

      pub fun reauthorize_owner() {
         // do penalties for transferring ownership here
         destroy self.gems.withdraw(amount: self.gems.balance / UFix64(2))

         self.authorizedAccount = self.owner!
      }

      destroy () {
         destroy self.owned_avatars
         destroy self.sold_avatars
         destroy self.gems
      }
    }



    pub resource GemAward {
      pub let num_gems : UFix64

      init(num_gems : UFix64) {
         self.num_gems = num_gems
      }
    }

    pub resource Map {
      pub let tiles : [Bool]
      pub let width : UInt8
      pub let height : UInt8

      init(width : UInt8, height : UInt8, tiles: [Bool])
      {
         assert(tiles.length == Int(width) * Int(height), message: "Incorred number of tiles for given dimensions!")
         self.width = width
         self.height = height
         self.tiles = tiles
      }
    }

    pub resource interface MapWithdraw {
      pub fun removeMap(at : UInt) : @Map 
    }

   pub resource interface MapCreate {
      pub fun createMap(width: UInt8, height: UInt8, tiles: [Bool]) : @Map 
    }

    pub resource interface MapInsert {
      pub fun insertMap(map: @Map)
    }

    pub resource interface MapBorrow {
      pub fun borrowMap(at: UInt) : &Map
    }
    
    pub resource GameProvider : MapWithdraw, MapCreate, MapInsert, MapBorrow {
      pub let maps : @[Map]
      access(self) let avatarMinter : Capability<auth &GachaAvatars.OriginalMinter>
      access(self) let gemsMinter : Capability<auth &GachaGems.Minter>

      init(avatarMinter : Capability<auth &GachaAvatars.OriginalMinter>, gemsMinter : Capability<auth &GachaGems.Minter>) {
         self.maps <- []
         self.avatarMinter = avatarMinter
         self.gemsMinter = gemsMinter
      }

      pub fun createMap(width: UInt8, height: UInt8, tiles: [Bool]) : @Map {
         return <- create Map(width: width, height: height, tiles: tiles)
      }

      pub fun removeMap(at : UInt) : @Map {
         return <-self.maps.remove(at: at)
      }

      pub fun insertMap(map : @Map) {
         self.maps.append(<-map)
      }

      pub fun borrowMap(at : UInt) : &Map {
         return &self.maps[at] as &Map
      }

      pub fun claimRewardsForScore(player : &Account{AwardAccount}, score: UInt32) {
         player.award_gems(award: <-self.gemsMinter.borrow()!.mintTokens(amount: UFix64(score)))
      }

      destroy() {
         destroy self.maps
      }
    }

    pub fun createNewAccount(name : String, authorizedOwner: PublicAccount, starting_avatar : Capability<&GachaAvatars.NFT>?) : @Account {
      return <-create Account(name: name, authorizedAccount: authorizedOwner)
    }

    init(avatarMinter : Capability<auth &GachaAvatars.OriginalMinter>, gemsMinter : Capability<auth &GachaGems.Minter>) {
      self.GameProviderStoragePath = /storage/MatchThreeMaps
      self.GameProviderPublicPath = /public/MatchThreeMaps

      self.AccountStoragePath = /storage/MatchThreeAccount
      self.AccountPublicPath = /public/MatchThreeAccount

      self.account.save(<- create GameProvider(avatarMinter: avatarMinter, gemsMinter: gemsMinter), to: self.GameProviderStoragePath)
      self.account.link<&AnyResource{MapBorrow}>(self.GameProviderPublicPath, target: self.GameProviderStoragePath)
    }
 }
 