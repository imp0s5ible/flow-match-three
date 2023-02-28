using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using DapperLabs.Flow.Sdk;
using DapperLabs.Flow.Sdk.Cadence;
using DapperLabs.Flow.Sdk.DataObjects;
using DapperLabs.Flow.Sdk.DevWallet;
using DapperLabs.Flow.Sdk.Unity;

using Cysharp.Threading.Tasks;

struct NFTView
{
    public uint id;
    public uint uuid;
    public string name;
    public string description;
    public string thumbnail;
    public string externalURL;
    public string collectionPublic;
    public string collectionPublicLinkedType;
    public string collectionProviderLinkedType;
    public string collectionName;
    public string collectionDescription;
    public string collectionExternalURL;
    public string collectionSquareImage;
    public string collectionBannerImage;
}

public class FlowGame : MonoBehaviour
{
    [SerializeField]
    private bool onDevAccount = false;
    [SerializeField]
    private UnityEngine.UI.Text createAccountScript = null;
    [SerializeField]
    private UnityEngine.UI.Text getNftViewScript = null;

    [SerializeField]
    private FlowGameStatus statusUpdater = null;

    private FlowControl.Account flowAccount = null;

    public void LoginRequest(string username)
    {
        Login(username).Forget();
    }

    public async UniTask<string> Login(string username)
    {
        bool isFinished = false;
        bool isSuccessful = false;
        string flowAddress = "";
        statusUpdater.UpdateStatus($"Connecting to {username}...");
        FlowSDK.GetWalletProvider().Authenticate(username, (a) => { flowAddress = a; isSuccessful = true; isFinished = true; }, () => { isFinished = true; });
        await UniTask.WaitUntil(() => isFinished);
        if (isSuccessful)
        {
            statusUpdater.UpdateStatus($"Connected to {username} at address {flowAddress}!");
            flowAccount = FlowControl.Data.Accounts.FirstOrDefault(x => x.AccountConfig["Address"] == flowAddress);
            return flowAddress;
        }
        else
        {
            statusUpdater.UpdateStatus($"Could not connect to {username}!");
            return "";
        }
    }

    // Start is called before the first frame update
    async void Start()
    {
        if (onDevAccount)
        {
            FlowSDK.RegisterWalletProvider(ScriptableObject.CreateInstance<DevWalletProvider>());
        }
        Debug.Assert(getNftViewScript != null);
    }

    async UniTask<NFTView?> GetNFTView(string address)
    {
        if (flowAccount == null)
        {
            statusUpdater.UpdateStatus("You need to log in before viewing NFT data!");
            return null;
        }

        FlowScriptResponse viewResult = await flowAccount.ExecuteScript(getNftViewScript.text, new CadenceString(address), new CadenceNumber(0));
        if (viewResult.Error != null)
        {
            statusUpdater.UpdateStatus($"Failed to get NFT data for {address}");
            return null;
        }

        CadenceComposite nftViewStruct = viewResult.Value as CadenceComposite;

        try
        {
            NFTView result = new NFTView();
            result.id = uint.Parse(nftViewStruct.CompositeFieldAs<CadenceNumber>("id").Value);
            result.uuid = uint.Parse(nftViewStruct.CompositeFieldAs<CadenceNumber>("uuid").Value);
            result.name = nftViewStruct.CompositeFieldAs<CadenceString>("name").Value;
            result.description = nftViewStruct.CompositeFieldAs<CadenceString>("description").Value;
            result.thumbnail = nftViewStruct.CompositeFieldAs<CadenceString>("thumbnail").Value;
            result.externalURL = nftViewStruct.CompositeFieldAs<CadenceString>("externalURL").Value;
            result.collectionPublic = nftViewStruct.CompositeFieldAs<CadenceString>("collectionPublic").Value;
            result.collectionPublicLinkedType = nftViewStruct.CompositeFieldAs<CadenceString>("collectionPublicLinkedType").Value;
            result.collectionProviderLinkedType = nftViewStruct.CompositeFieldAs<CadenceString>("collectionProviderLinkedType").Value;
            result.collectionName = nftViewStruct.CompositeFieldAs<CadenceString>("collectionName").Value;
            result.collectionDescription = nftViewStruct.CompositeFieldAs<CadenceString>("collectionDescription").Value;
            result.collectionExternalURL = nftViewStruct.CompositeFieldAs<CadenceString>("collectionExternalURL").Value;
            result.collectionSquareImage = nftViewStruct.CompositeFieldAs<CadenceString>("collectionSquareImage").Value;
            result.collectionBannerImage = nftViewStruct.CompositeFieldAs<CadenceString>("collectionBannerImage").Value;
            return result;
        }
        catch (System.Exception e)
        {
            statusUpdater.UpdateStatus($"Failed to parse info on NFT collection: {e.Message}");
            return null;
        }

    }
}
