 import GachaAvatars from "gacha-avatars.cdc" 
 import GachaGems from "gacha-gems.cdc"

 pub contract GachaGame {
    
    pub let AccountStoragePath : StoragePath
    pub let AccountPublicPath : PublicPath

    pub let GameProviderStoragePath : StoragePath
    pub let GameProviderPublicPath : PublicPath

    pub resource interface ReadAccount {
      pub fun getName() : String
      pub fun borrowAvatar(uuid: UInt64) : &GachaAvatars.NFT?
      pub fun avatarUUIDs() : [UInt64]
      pub fun isAvatarValid(uuid: UInt64) : Bool
      {
         return self.borrowAvatar(uuid: uuid) != nil
      }
    }

    pub resource interface CreditAccountAvatars {
      access(contract) fun addAvatar(avatar : @GachaAvatars.NFT)
    }

    pub resource interface BuyAccountAvatars {
      access(contract) fun buyAvatarFor(uuid: UInt64, payment : @GachaGems.Vault, forAccount: Capability<&GachaGame.Account{CreditAccountAvatars}>)
      pub fun getAvatarPrice(uuid: UInt64) : UFix64?
    }

    pub resource interface ConfigureAccount {
      pub fun setName(newName : String)
      pub fun setAvatar(uuid : UInt64)
      pub fun sellAvatar(uuid : UInt64, sellPrice : UFix64)
    }

    pub resource interface AwardAccount {
      access(contract) fun awardGems(award : @GachaGems.Vault)
    }

    pub resource Account : ReadAccount, ConfigureAccount, AwardAccount, CreditAccountAvatars, BuyAccountAvatars {
      priv var name : String
      priv var authorizedAccount : PublicAccount
      priv var activeAvatarUUID : UInt64
      priv var gems : @GachaGems.Vault
      priv var ownedAvatars : @{UInt64: GachaAvatars.NFT}
      priv var sellPrices : {UInt64: UFix64}
      priv var soldAvatars : @{UInt64: GachaAvatars.NFT}

      init(name : String, authorizedAccount: PublicAccount)
      {
         self.name = name
         self.authorizedAccount = authorizedAccount
         self.activeAvatarUUID = 0
         self.sellPrices = {}
         self.gems <- GachaGems.createEmptyVault()
         self.ownedAvatars <- {}
         self.soldAvatars <- {}
      }

      pub fun isOwnedByAuthorizedAccount(): Bool {
         return self.authorizedAccount.address == self.owner!.address
      }

      pub fun getName() : String {
         return self.name
      }

      pub fun borrowAvatar(uuid: UInt64) : &GachaAvatars.NFT? {
         assert(self.isOwnedByAuthorizedAccount())
         return &self.ownedAvatars[uuid] as &GachaAvatars.NFT?
      }

      pub fun avatarUUIDs() : [UInt64] {
         return self.ownedAvatars.keys
      }

      pub fun setName(newName : String) {
         assert(self.isOwnedByAuthorizedAccount())
         self.name = newName
      }

      pub fun setAvatar(uuid : UInt64)
      {
         assert(self.isOwnedByAuthorizedAccount())
         if (uuid != 0) {
            assert(self.ownedAvatars.containsKey(uuid))
         }
         self.activeAvatarUUID = uuid
      }

      pub fun buyAvatarFrom(uuid: UInt64, fromAccount: Capability<&GachaGame.Account{BuyAccountAvatars}>, forAccount: Capability<&GachaGame.Account{CreditAccountAvatars}>)
      {
         assert(self.isOwnedByAuthorizedAccount())
         assert(forAccount.borrow()!.uuid == self.uuid)

         let payment <- self.gems.withdraw(amount: fromAccount.borrow()!.getAvatarPrice(uuid: uuid)!) as! @GachaGems.Vault
         fromAccount.borrow()!.buyAvatarFor(uuid: uuid, payment: <-payment, forAccount: forAccount)
      }

      access(contract) fun buyAvatarFor(uuid: UInt64, payment : @GachaGems.Vault, forAccount: Capability<&GachaGame.Account{CreditAccountAvatars}>) {
         assert(self.isOwnedByAuthorizedAccount())
         assert(payment.balance >= self.sellPrices[uuid]!, message: "not enough gems to buy this avatar!")
         self.gems.deposit(from: <-payment)
         var boughtAvatar: @GachaAvatars.NFT <- self.soldAvatars.remove(key: uuid)!
         forAccount.borrow()!.addAvatar(avatar: <-boughtAvatar)
      }

      pub fun getAvatarPrice(uuid: UInt64) : UFix64? {
         assert(self.isOwnedByAuthorizedAccount())
         return self.sellPrices[uuid]
      }

      pub fun sellAvatar(uuid : UInt64, sellPrice : UFix64) {
         assert(self.isOwnedByAuthorizedAccount())
         let avatarToSell <- self.ownedAvatars.remove(key:uuid)!
         assert(avatarToSell.minimumResalePrice <= sellPrice);
         let sellUUID = avatarToSell.uuid

         self.soldAvatars[sellUUID] <-! avatarToSell
      }

      pub fun removeAvatarFromSale(uuid: UInt64) {
         assert(self.isOwnedByAuthorizedAccount())
         self.ownedAvatars[uuid] <-! self.soldAvatars.remove(key: uuid)
      }

      access(contract) fun addAvatar(avatar : @GachaAvatars.NFT){
         assert(self.isOwnedByAuthorizedAccount())
         self.ownedAvatars[avatar.uuid] <-! avatar
      }

      access(contract) fun awardGems(award : @GachaGems.Vault) {
         assert(self.isOwnedByAuthorizedAccount())
         self.gems.deposit(from: <-award)
      }

      pub fun reauthorizeOwner() {
         // do penalties for transferring ownership here
         destroy self.gems.withdraw(amount: self.gems.balance / UFix64(2))

         self.authorizedAccount = self.owner!
      }

      destroy () {
         destroy self.ownedAvatars
         destroy self.soldAvatars
         destroy self.gems
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
         player.awardGems(award: <-self.gemsMinter.borrow()!.mintTokens(amount: UFix64(score)))
      }

      destroy() {
         destroy self.maps
      }
    }

    pub fun createNewAccount(name : String, authorizedOwner: PublicAccount, startingAvatar : Capability<&GachaAvatars.NFT>?) : @Account {
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
 