 import NonFungibleToken from "../dependencies/flow-nft/contracts/NonFungibleToken.cdc"
 import FungibleToken from "../dependencies/flow-nft/contracts/utility/FungibleToken.cdc"
 import MetadataViews from "../dependencies/flow-nft/contracts/MetadataViews.cdc"

 pub contract MatchThreeAvatars : NonFungibleToken {
    pub let CollectionStoragePath: StoragePath
    pub let CollectionPublicPath: PublicPath
    pub let OriginalMinterStoragePath: StoragePath
    pub let ConfiguratorStoragePath: StoragePath
    
    pub var avatarRoyalties: MetadataViews.Royalties
    pub var totalSupply: UInt64
    pub var collectionLogoURL : String
    pub var collectionLogoMimeType : String
    pub var nextId : UInt64

    pub event ContractInitialized()
    pub event Withdraw(id: UInt64, from: Address?)
    pub event Deposit(id: UInt64, to: Address?)

    pub resource NFT : NonFungibleToken.INFT, MetadataViews.Resolver {
        pub let id: UInt64
        pub let name: String
        pub let description: String
        pub let portraitUrl : String
        pub let mintedBy : Address
        pub let mintedAt : UInt64

        init(
            minter: Address,
            name: String,
            description: String,
            portraitUrl: String
        ) {
            self.mintedBy = minter
            self.mintedAt = getCurrentBlock().height
            self.id = MatchThreeAvatars.nextId
            self.name = name
            self.description = description
            self.portraitUrl = portraitUrl

            MatchThreeAvatars.nextId = MatchThreeAvatars.nextId + 1
            MatchThreeAvatars.totalSupply = MatchThreeAvatars.totalSupply + 1
        }

        pub fun getViews() : [Type] {
            return [
                Type<MetadataViews.Display>(),
                Type<MetadataViews.Editions>(),
                Type<MetadataViews.Serial>(),
                Type<MetadataViews.Royalties>(),
                Type<MetadataViews.NFTCollectionData>(),
                Type<MetadataViews.NFTCollectionDisplay>(),
                Type<MetadataViews.Traits>()
            ]
        }

        pub fun resolveView(_ view: Type): AnyStruct? {
            switch view {
                case Type<MetadataViews.Display>():
                    return MetadataViews.Display(
                        name: self.name,
                        description: self.description,
                        thumbnail: MetadataViews.HTTPFile(
                            url: self.portraitUrl
                        )
                    )
                case Type<MetadataViews.Editions>():
                    let editionInfo = MetadataViews.Edition(name: "Matcher Avatars", number: self.id, max: nil)
                    let editionList: [MetadataViews.Edition] = [editionInfo]
                    return MetadataViews.Editions(
                        editionList
                    )
                case Type<MetadataViews.Serial>():
                    return MetadataViews.Serial(
                        self.id
                    )
                case Type<MetadataViews.Royalties>():
                    return MatchThreeAvatars.avatarRoyalties;
                case Type<MetadataViews.ExternalURL>():
                    return MetadataViews.ExternalURL("https://example-nft.onflow.org/".concat(self.id.toString()))
                case Type<MetadataViews.NFTCollectionData>():
                    return MetadataViews.NFTCollectionData(
                        storagePath: MatchThreeAvatars.CollectionStoragePath,
                        publicPath: MatchThreeAvatars.CollectionPublicPath,
                        providerPath: /private/exampleNFTCollection,
                        publicCollection: Type<&MatchThreeAvatars.Collection{NonFungibleToken.CollectionPublic}>(),
                        publicLinkedType: Type<&MatchThreeAvatars.Collection{NonFungibleToken.CollectionPublic,NonFungibleToken.Receiver,MetadataViews.ResolverCollection}>(),
                        providerLinkedType: Type<&MatchThreeAvatars.Collection{NonFungibleToken.CollectionPublic,NonFungibleToken.Provider,MetadataViews.ResolverCollection}>(),
                        createEmptyCollectionFunction: (fun (): @NonFungibleToken.Collection {
                            return <-MatchThreeAvatars.createEmptyCollection()
                        })
                    )
                case Type<MetadataViews.NFTCollectionDisplay>():
                    let logo = MetadataViews.Media(
                        file: MetadataViews.HTTPFile(
                            url: "https://assets.website-files.com/5f6294c0c7a8cdd643b1c820/5f6294c0c7a8cda55cb1c936_Flow_Wordmark.svg"
                        ),
                        mediaType: "image/svg+xml"
                    )
                    return MetadataViews.NFTCollectionDisplay(
                        name: "Match Three Avatar Collection",
                        description: "This collection includes cute anime avatars you can use in the Match Three game!",
                        externalURL: MetadataViews.ExternalURL(""),
                        squareImage: logo,
                        bannerImage: logo,
                        socials: {}
                    )
                case Type<MetadataViews.Traits>():
                    let traitsView = MetadataViews.dictToTraits(dict: {"mintedBy": self.mintedBy}, excludedNames: [])

                    let mindetAtTrati = MetadataViews.Trait(name: "mintedAt", value: self.mintedAt, displayType: "Date", rarity: nil)
                    traitsView.addTrait(mindetAtTrati)

                    return traitsView

            }
            return nil
        }

        destroy() {
            MatchThreeAvatars.totalSupply = MatchThreeAvatars.totalSupply - 1
        }
    }

    pub resource Collection : NonFungibleToken.Provider, NonFungibleToken.Receiver, NonFungibleToken.CollectionPublic, MetadataViews.ResolverCollection
    {
        pub var ownedNFTs: @{UInt64: NonFungibleToken.NFT}

        init () {
            self.ownedNFTs <- {}
        }

        pub fun withdraw(withdrawID: UInt64): @NonFungibleToken.NFT {
            let avatar <- self.ownedNFTs.remove(key: withdrawID) ?? panic ("Avatar with given ID not found!")
            emit Withdraw(id: avatar.id, from: self.owner?.address)

            return <-(avatar !as @NonFungibleToken.NFT)
        }

        pub fun deposit(token : @NonFungibleToken.NFT) {
            let avatar <- token as! @MatchThreeAvatars.NFT
            emit Deposit(id: avatar.id, to: self.owner?.address)

            self.ownedNFTs[avatar.id] <-! avatar
        }

        pub fun getIDs(): [UInt64] {
            return self.ownedNFTs.keys
        }

        pub fun borrowNFT(id: UInt64): &NonFungibleToken.NFT {
            return (&self.ownedNFTs[id] as &NonFungibleToken.NFT?)!
        }

        pub fun borrowViewResolver(id: UInt64): &AnyResource{MetadataViews.Resolver} {
            let nft = (&self.ownedNFTs[id] as auth &NonFungibleToken.NFT?)!
            let avatar = nft as! &MatchThreeAvatars.NFT
            return avatar
        }

        destroy() {
            destroy self.ownedNFTs
        }
    }

    pub resource Configurator {
        pub fun setCollectionLogoURL(logoUrl : String, mimeType : String)
        {
            MatchThreeAvatars.collectionLogoURL = logoUrl
            MatchThreeAvatars.collectionLogoMimeType = mimeType
        }

        pub fun setAvatarRoyalties(royalties : MetadataViews.Royalties)
        {
            MatchThreeAvatars.avatarRoyalties = royalties
        }
    }

    pub resource OriginalMinter {

        pub fun mintOriginalAvatar(
            name: String,
            description: String,
            portraitUrl: String,
        ): @NFT {
            return <- create MatchThreeAvatars.NFT(
            minter: self.owner!.address,
            name: name,
            description: description,
            portraitUrl: portraitUrl
        )
        }
    }

    pub fun createEmptyCollection(): @Collection {
        return <- create Collection()
    }
 
    init (collectionLogoURL : String, collectionLogoMimeType: String, avatarRoyalties : MetadataViews.Royalties)
    {
        self.CollectionStoragePath = /storage/MatchThreeAvatars
        self.CollectionPublicPath = /public/MatchThreeAvatars
        self.OriginalMinterStoragePath = /storage/MatchThreeAvatarsOriginalMinter
        self.ConfiguratorStoragePath = /storage/MatchThreeAvatarsConfigurator
    
        self.avatarRoyalties = avatarRoyalties
        self.totalSupply = 0
        self.nextId = 0

        self.collectionLogoURL = collectionLogoURL
        self.collectionLogoMimeType = collectionLogoMimeType

        self.account.save(<-create Collection(), to: self.CollectionStoragePath)
        self.account.link<&Collection{NonFungibleToken.CollectionPublic, MetadataViews.ResolverCollection}>(self.CollectionPublicPath, target: self.CollectionStoragePath)

        self.account.save(<-create OriginalMinter(), to: self.OriginalMinterStoragePath)
        self.account.save(<-create Configurator(), to: self.ConfiguratorStoragePath)
    }
 }