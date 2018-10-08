using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using NBitcoin.Protocol;
using System.IO; // used for testing
using System.Net;
using QBitNinja;

namespace bitcoin
{
    public enum WalletType { HotWallet, VaultWallet, ExchangeWallet };
    public enum TickerSymbol { BTC, LTC, XMR, ETH };

    public class ExampleCode
    {
        public void BuildAndSend()
        {
            WalletManager wm = new WalletManager(); //new WalletManager(Network.TestNet, new Node(...));

            string customerID = "customerID"; //GUID

            string WalletAddress = wm.CreateWallet(WalletType.VaultWallet, customerID, TickerSymbol.BTC);

            string sendToAddress = "someBitcoinAddressHere";
            decimal amountToSend = 0.1m; ///0.1m == 0.1BTC



           string response = wm.SendToAddress(
                                    amountToSend, 
                                    sendToAddress, 
                                    customerID, 
                                    TickerSymbol.BTC, 
                                    WalletType.VaultWallet
                                    );
        }
    }

    public static class WalletUtilities
    {

        public static decimal GetMinerFee(TickerSymbol Symbol)
        {
            int satPerByte = 10;
            int BytesEstimate = 300;

            return new Money(satPerByte * BytesEstimate).ToDecimal(MoneyUnit.BTC); // implement later
        }

        public static string GetBitfoldAddress(TickerSymbol Symbol)
        {
            return "[address here]"; // get from database later.
        }

        public static decimal GetConversionRate(TickerSymbol Current, TickerSymbol Target = TickerSymbol.USD)
        {
            double cur = 1;
            double trg = 1;

            cur = GetRateInUSD(Current);
            if (Target != TickerSymbol.USD) trg = GetRateInUSD(Target);

            double convert = -1;

            if (cur > 0 && trg > 0) // check for incorrect response
            {
                convert = cur / trg; // if A = $10 and B = $1 then return 10 as the number of TargetSymbol to be recieved upon conversion
            }

            double rounded = Math.Floor(convert * 2) / 2;

            return (decimal)rounded;
        }

        private static double GetRateInUSD(TickerSymbol Symbol)
        {
            WebRequest cbRequest = null;

            if (Symbol == TickerSymbol.BTC)
            {
                cbRequest = WebRequest.Create("https://api.coinbase.com/v2/prices/BTC-USD/sell");  //using coinbase api
            }
            else if (Symbol == TickerSymbol.ETH)
            {
                cbRequest = WebRequest.Create("https://api.coinbase.com/v2/prices/ETH-USD/sell");  //using coinbase api
            }
            else if (Symbol == TickerSymbol.LTC)
            {
                cbRequest = WebRequest.Create("https://api.coinbase.com/v2/prices/LTC-USD/sell");  //using coinbase api

            }
            else
            {
                return -1;
            }

            cbRequest.Method = "GET";
            cbRequest.Headers["CB-VERSION"] = "2017-05-19";

            WebResponse cbResponse = cbRequest.GetResponse();

            Stream rStream = cbResponse.GetResponseStream();
            StreamReader reader = new StreamReader(rStream);

            string response = reader.ReadToEnd();
            ExchangeInfo eInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ExchangeInfo>(response);

            double bitCoinAmount = eInfo.data.amount;

            reader.Close();
            rStream.Close();
            cbResponse.Close();

            return bitCoinAmount;
        }
     
        public static double GetConversionRate(TickerSymbol Current, TickerSymbol Target)
        {
            double cur = GetRateInUSD(Current);
            double trg = GetRateInUSD(Target);

            double convert = -1;

            if(cur > 0 && trg > 0) // check for incorrect response
            {
                convert = cur / trg; // if A = $10 and B = $1 then return 10 as the number of TargetSymbol to be recieved upon conversion
            }

            return convert;
        }

        public class ExchangeInfo
        {
            public JsonNesting data { get; set; }

        }
        public class JsonNesting
        {
            public string currency { get; set; }
            public float amount { get; set; }
        }

    }

    public class WalletManager
    {
        private Network net;
        private Node netNode;

        private WalletData walletDat;

        public WalletManager()
        {
            net = Network.Main;
            walletDat = new WalletData();
            netNode = null;
        }

        public WalletManager(Network network, Node ConnectedNode)
        {
            net = network;
            walletDat = new WalletData();
            netNode = ConnectedNode;
        }
        
        public string CreateWallet(WalletType Wallet, string ID, TickerSymbol Symbol = TickerSymbol.BTC)
        {
            if (walletDat.Exists(ID, Wallet, Symbol))
            {
                return GetAddress(Wallet, ID, Symbol); // if wallet already exists get current address
            }
            else
            {
                if(Wallet == WalletType.HotWallet)
                {
                    return string.Empty; // will add code later
                }
                else if (Wallet == WalletType.VaultWallet)
                {
                    return VaultWalletCreate(ID, Symbol);
                }
                else if (Wallet == WalletType.ExchangeWallet)
                {
                    return string.Empty; // will add code later
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string GetAddress(WalletType Wallet, string ID, TickerSymbol Symbol = TickerSymbol.BTC)
        {
            if(Wallet == WalletType.VaultWallet)
            {
                WalletStore ws = GetVaultWallet(ID + ".wkey", Symbol);

                Key nKey = new Key(ws.Key);

                BitcoinSecret bitSecret = new BitcoinSecret(nKey, net); 
                BitcoinAddress bitAddress = bitSecret.PubKey.GetAddress(net); 

                return bitAddress.ToString(); // returns recieveing address.

            }
            else
            {
                return "";
            }
        }

        public List<string> GetAddressList()
        {
            return null; // change for implementation of Master Wallet
        }

        public string CreateAddress(WalletType Wallet, string ID, TickerSymbol Symbol = TickerSymbol.BTC)
        {
            if (walletDat.Exists(ID, Wallet))
            {
                return GetAddress(Wallet, ID, Symbol);
            }
            else
            {
                //create wallets if there is no existing wallet
                if (Wallet == WalletType.ExchangeWallet)
                {
                    return ExchangeWalletCreate(ID, Symbol);
                }
                else if (Wallet == WalletType.HotWallet)
                {
                    return HotWalletCreate(ID, Symbol);
                }
                else if (Wallet == WalletType.VaultWallet)
                {
                    return VaultWalletCreate(ID, Symbol);
                }
                else
                {
                    return "NO WALLET CREATED"; // probably add error codes later
                }
            }                 
        }

        private string VaultWalletCreate(string ID, TickerSymbol Symbol, string[] Attributes = null)
        {
            Key nKey = new Key(); //generate new key

            BitcoinSecret bitSecret = new BitcoinSecret(nKey, net); // create secret
            BitcoinAddress bitAddress = bitSecret.PubKey.GetAddress(net); // create address from secret

            walletDat.SaveVault(ID, nKey.ToBytes(), Attributes); // saves Data

            return bitAddress.ToString(); // returns recieveing address.
        }

        private WalletStore GetVaultWallet(string ID, TickerSymbol Symbol)
        {
            byte[] key = walletDat.LoadVault(ID);
            return new WalletStore { Key = key };
        }

        public string SendToAddress(decimal Amount, string Address, string ID, TickerSymbol Symbol, WalletType Wallet, int MinerFee = -1)
        {
            string result = string.Empty;

            if (MinerFee < 0)
            {
               MinerFee = WalletUtilities.GetMinerFee(Symbol);
            }

            if(Symbol == TickerSymbol.BTC)
            {
                if(Wallet == WalletType.VaultWallet)
                {
                    Key wKey = new Key(walletDat.LoadVault(ID));

                    BitcoinSecret bitSecret = wKey.GetBitcoinSecret(net); //your wallet
                    BitcoinAddress bitAddress = bitSecret.PubKey.GetAddress(net); //your address

                    BitcoinAddress SendAddress = BitcoinAddress.Create(Address, net); // address you are sending to

                    Transaction tx = new Transaction();


                    Coin[] CoinPurse = walletDat.GetCoinsByAddress(bitAddress, net, 6).ToArray();

                    TransactionBuilder txBuilder = new TransactionBuilder();
                    Transaction builtTx = new Transaction();

                    builtTx = txBuilder
                        .AddCoins(CoinPurse)
                        .AddKeys(wKey)
                        .Send(SendAddress, new Money(Amount, MoneyUnit.BTC))
                        .SendFees(MinerFee)
                        .SetChange(bitAddress)
                        .BuildTransaction(true);

                    builtTx = txBuilder.SignTransaction(builtTx); // sign the transaction
                    bool very = txBuilder.Verify(builtTx);        // verify the transaction for sending

                    if (netNode == null)
                    {
                        using (Node node = Node.ConnectToLocal(net)) //Connect to local if no node is set
                        {
                            node.VersionHandshake();

                            node.SendMessage(new InvPayload(InventoryType.MSG_TX, builtTx.GetHash()));

                            node.SendMessage(builtTx.CreatePayload()); // broadcast message to send funds
                            System.Threading.Thread.Sleep(500); //Wait a bit
                        }
                    }
                    else
                    {
                        using (Node node = netNode)
                        {
                            node.VersionHandshake();

                            node.SendMessage(new InvPayload(InventoryType.MSG_TX, builtTx.GetHash()));

                            node.SendMessage(builtTx.CreatePayload()); // broadcast message to send funds
                            System.Threading.Thread.Sleep(500); //Wait a bit
                        }
                    }

                    if (very) result = "BTC VERIFIED"; // need better error codes / error catching
                    else result = "BTC ERROR";
                }
            }

            return result;
        }

        public class WalletStore
        {
            public string ID { get; set; }
            public int TimeStamp { get; set; }
            public string[] Attributes { get; set; }
            public byte[] Key { get; set; }
        }

        public class WalletData
        {
            public WalletData()
            {
                //setup
                //needs database info here
            }

            public List<string> GetTransactions(string Address)
            {
               /* BitcoinAddress ba = BitcoinAddress.Create(Address);

                var node = Node.ConnectToLocal(Network.Main);
                node.VersionHandshake();
                var chain = node.GetChain();*/

                return null;
            }

            public void SaveData(string ID, byte[] bytes)
            {
                File.WriteAllBytes(ID + ".wkey", bytes); // temp save for testing
            }

            public void SaveVault(string ID, byte[] keyBytes, string[] attributes)
            {
                SaveData(ID, keyBytes); // temp save for testing
            }

            public byte[] LoadVault(string ID)
            {
                return File.ReadAllBytes(ID + ".wkey"); // temp save for testing
            }

            public bool Exists(string ID, WalletType Wallet, TickerSymbol Symbol = TickerSymbol.BTC)
            {
                return File.Exists(ID + ".wkey");  // temp save for testing
            }

            public List<Coin> GetCoinsByAddress(BitcoinAddress Address, Network net = null, int confirmations = 6)
            {
                QBitNinja.Client.QBitNinjaClient cl = new QBitNinja.Client.QBitNinjaClient(net);
                QBitNinja.Client.Models.BalanceModel bm = cl.GetBalance(new BitcoinPubKeyAddress(Address.ToString())).Result;

                List<Coin> txs = new List<Coin>();

                foreach (var operation in bm.Operations)
                {
                    foreach(var cn in operation.ReceivedCoins)
                    {
                        if (operation.Confirmations >= confirmations)
                        {
                            Coin C = (Coin)cn;
                            txs.Add(C);
                        }
                    }
                }

                return txs;
            }
        }
    }
}
