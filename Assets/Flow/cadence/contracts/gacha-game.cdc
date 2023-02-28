 import GachaAvatars from "gacha-avatars.cdc" 
 import GachaGems from "gacha-gems.cdc"

 pub contract GachaGame {
    
    pub let AccountStoragePath : StoragePath
    pub let AccountPublicPath : PublicPath

    pub let GameProviderStoragePath : StoragePath
    pub let GameProviderPublicPath : PublicPath

    pub resource interface ReadAccount {
      pub fun get_name() : String
      pub fun borrow_avatar() : &GachaAvatars.NFT?
      pub fun is_avatar_valid() : Bool {
         return self.borrow_avatar() != nil
      }
    }

    pub resource AvatarSellOffer {
      pub let avatar : @GachaAvatars.NFT?
      pub let price : UFix64
      
      init(avatar : @GachaAvatars.NFT, price : UFix64) {
         self.avatar <- avatar
         self.price = price
      }

      destroy () {
         destroy self.avatar
      }
    }

    pub resource interface ConfigureAccount {
      pub fun set_name(new_name : String)
      pub fun set_avatar(avatar_index : Int)
      pub fun sell_avatar(avatar_index : Int, price : UFix64) : @GachaGame.AvatarSellOffer;
    }

    pub resource interface CreditAccountAvatars {
      pub fun add_avatar(new_avatar : @GachaAvatars.NFT)
    }

    pub resource interface AwardAccount {
      pub fun award_gems(award : @GemAward)
    }

    pub resource Account : ReadAccount, ConfigureAccount, AwardAccount, CreditAccountAvatars {
      pub var name : String
      pub var num_gems : UFix64
      pub var owned_avatars : @[GachaAvatars.NFT]
      pub var avatar_index : Int

      init(name : String)
      {
         self.name = name
         self.num_gems = 0.0
         self.avatar_index = -1
         self.owned_avatars <- []
      }

      pub fun get_name() : String {
         return self.name
      }

      pub fun borrow_avatar() : &GachaAvatars.NFT? {
         if (0 < self.avatar_index) {
            return &self.owned_avatars[self.avatar_index] as &GachaAvatars.NFT
         } else {
            return nil
         }
      }

      pub fun set_name(new_name : String) {
         self.name = new_name
      }

      pub fun set_avatar(avatar_index : Int)
      {
         if (avatar_index == -1) {
            self.avatar_index = avatar_index
         }
         assert(avatar_index == -1 || (0 <= avatar_index && avatar_index < self.owned_avatars.length));

         self.avatar_index = avatar_index
      }

      pub fun buy_avatar(offer : @GachaGame.AvatarSellOffer) {
         assert(self.num_gems >= offer.price, message: "not enough gems to buy this avatar!")
         self.num_gems = self.num_gems - offer.price;
         var tmp = nil
         destroy offer
      }

      pub fun sell_avatar(avatar_index : Int, sell_price : UFix64) : @GachaGame.AvatarSellOffer {
         let avatar_to_sell <- self.owned_avatars.remove(at:avatar_index)
         return <- create GachaGame.AvatarSellOffer (<-avatar_to_sell, sell_price)
      }

      pub fun add_avatar(avatar : @GachaAvatars.NFT){
         self.owned_avatars.append(<-avatar);
      }

      pub fun award_gems(award : @GemAward) {
         self.num_gems = self.num_gems + award.num_gems
         destroy award
      }

      destroy () {
         destroy self.owned_avatars
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
      pub let avatar_minter : auth &GachaAvatars.OriginalMinter
      pub let gems_minter : auth &GachaGems.Minter

      init(avatar_minter : auth &GachaAvatars.OriginalMinter, gems_minter : auth &GachaGems.Minter) {
         self.maps <- []
         self.avatar_minter = avatar_minter
         self.gems_minter = gems_minter
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
         player.award_gems(award: <- create GemAward(num_gems: UFix64(score)))
      }

      destroy() {
         destroy self.maps
      }
    }

    pub fun createNewAccount(name : String, starting_avatar : Capability<&GachaAvatars.NFT>?) : @Account {
      return <-create Account(name: name)
    }

    init() {
      self.GameProviderStoragePath = /storage/MatchThreeMaps
      self.GameProviderPublicPath = /public/MatchThreeMaps

      self.AccountStoragePath = /storage/MatchThreeAccount
      self.AccountPublicPath = /public/MatchThreeAccount

      self.account.save(<- create GameProvider(), to: self.GameProviderStoragePath)
      self.account.link<&AnyResource{MapBorrow}>(self.GameProviderPublicPath, target: self.GameProviderStoragePath)
    }
 }
 