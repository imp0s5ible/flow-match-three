import GachaAvatars from "../contracts/gacha-avatars.cdc"
import GachaGame from "../contracts/gacha-game.cdc"
import NonFungibleToken from "../dependencies/flow-nft/contracts/NonFungibleToken.cdc"
import MetadataViews from "../dependencies/flow-nft/contracts/MetadataViews.cdc"

/// This transaction is for transferring and NFT from
/// one account to another

transaction(username : String) {

    let forAccount : AuthAccount

    prepare(forAccount : AuthAccount) {
        self.forAccount = forAccount
    }

    execute {
        let newAccount:  @GachaGame.Account <- GachaGame.createNewAccount(name: username, authorizedOwner: getAccount(self.forAccount.address), startingAvatar: nil)
        self.forAccount.save(<-newAccount, to: GachaGame.AccountStoragePath)
        self.forAccount.link<&GachaGame.Account{GachaGame.ReadAccount}>(GachaGame.AccountPublicPath, target: GachaGame.AccountStoragePath)
    }
}