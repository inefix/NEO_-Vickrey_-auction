using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

namespace NEO
{
    public class VickreyAuction_old3 : SmartContract
    {
        private static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash(); //Owner Address
        private static readonly BigInteger TotalSupplyValue = 100000000000;     //1000 VNEO

        [DisplayName("name")]
        public static string Name() => "VNEO"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "VNEO"; //symbol of the token

        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        [DisplayName("supportedStandards")]
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(Owner);
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "BalanceOf") return BalanceOf((byte[])args[0]);

                //if (method == "Decimals") return Decimals();

                if (method == "Deploy") return Deploy();

                //if (method == "Name") return Name();

                //if (method == "Symbol") return Symbol();

                //if (method == "SupportedStandards") return SupportedStandards();

                if (method == "TotalSupply") return TotalSupply();

                //if (method == "Transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2], ExecutionEngine.CallingScriptHash);

                if (method == "Init") return Init((string)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4]);

                if (method == "Announce") return Announce((byte[])args[0]);

                if (method == "Bid") return Bid((byte[])args[0], (string)args[1]);

                //if (method == "Claim") return Claim();

                if (method == "Result") return Result((byte[])args[0]);

                if (method == "End") return End();

                if (method == "Reveal") return Reveal((byte[])args[0], (int)args[1], (int)args[2]);

                //if (method == "Withdraw") return Withdraw();

                //if (method == "GetReceiver") return GetReceiver();

                //if (method == "GetSender") return GetSender();

                //if (method == "GetTime") return GetCurrentTime();

                if (method == "GenerateHash") return GenerateSHA256((int)args[0], (int)args[1]);
            }
            return false;
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
                throw new InvalidOperationException("The parameter account SHOULD be 20-byte addresses.");
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            return asset.Get(account).AsBigInteger();
        }

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return contract.Get("totalSupply").AsBigInteger();
        }

        [DisplayName("deploy")]
        public static bool Deploy()
        {
            if (!Runtime.CheckWitness(Owner)) return false;
            if (TotalSupply() != 0) return false;
            Storage.Put("totalSupply", TotalSupplyValue);
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            asset.Put(Owner, TotalSupplyValue);
            Transferred(null, Owner, TotalSupplyValue);
            return true;
        }

        private static bool Init(string secret, BigInteger reservePrice, int durationBidding, int durationReveal, int durationResulting)
        {
            if (!Runtime.CheckWitness(Owner)) return false;

            //Auction
            Auction auction = new Auction(secret, Runtime.Time, durationBidding, durationReveal, durationResulting, reservePrice, Owner);
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
            //Storage.Put(Storage.CurrentContext, "auction", Helper.Serialize(auction));
            Storage.Put("auction", Helper.Serialize(auction));
            return true;
        }

        private static bool Announce(byte[] address)
        {
            if (Runtime.CheckWitness(Owner)) return false;
            Runtime.Notify("0");
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            Auction auction = (Auction)Helper.Deserialize(Storage.Get("auction"));
            Runtime.Notify("1");
            uint now = Runtime.Time;
            Runtime.Notify("11");
            uint test = auction.endOfBidding;
            Runtime.Notify("12");
            if (now >= test) return false;
            Runtime.Notify("2");
            if (!auction.AnnounceBidder(address)) return false;
            Runtime.Notify("3");
            Transferred(null, address, 1000);
            Runtime.Notify("4");
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
            Storage.Put("auction", Helper.Serialize(auction));
            Runtime.Notify("5");
            Storage.Put("totalSupply", BytesToBigInteger(Storage.Get("totalSupply")) + 1000);
            return true;
        }

        private static bool Bid(byte[] bidderAddress, string hash)
        {
            if (Runtime.CheckWitness(Owner)) return false;

            //Deserialize auction
            Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            if (Runtime.Time >= auction.endOfBidding) return false;

            //Store bidder's info
            auction.SetBiderHash(bidderAddress, hash);
            Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());

            return true;
        }

        private static bool Reveal(byte[] senderAddress, int stake, int nonce)
        {
            //Get auction
            Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            uint now = Runtime.Time;
            if (now < auction.endOfBidding || now >= auction.endOfRevealing) return false;

            //Get bidder's hash
            string hash = auction.GetBidderHash(senderAddress);
            if (hash == null) return false;

            //Compute hash and compare
            string generatedHash = GenerateSHA256(stake, nonce);
            if (hash != generatedHash) return false;

            auction.SetBiderStake(senderAddress, stake);

            //Confirm reveal of bidder
            auction.ConfirmReveal(senderAddress);
            Transferred(senderAddress, null, stake);

            if (stake > auction.highestBid)
            {
                auction.secondBid = auction.highestBid;
                auction.highestBid = stake;
                auction.higherBidder = senderAddress;
            }
            else if (stake > auction.secondBid)
            {
                auction.secondBid = stake;
            }
            Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());

            return true;
        }

        private static string Result(byte[] senderAddress)
        {
            Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            uint now = Runtime.Time;
            if (now < auction.endOfRevealing || now >= auction.endOfResulting) return "wait";
            if (Runtime.CheckWitness(auction.higherBidder))
            {
                //create variable to know if highBidder has called result
                auction.hasResulted = true;
                Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
                Transferred(auction.higherBidder, Owner, auction.secondBid);
                return auction.secret;
            }
            else
            {
                Transferred(null, senderAddress, auction.GetBiderStake(senderAddress));
            }
            return "true";
        }

        private static bool End()
        {
            if (!Runtime.CheckWitness(Owner)) return false;
            Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            if (Runtime.Time < auction.endOfResulting) return false;
            if (auction.hasResulted == false)
            {
                Transferred(auction.higherBidder, Owner, auction.secondBid);
            }
            return true;
        }

        private static string BytesToString(byte[] data) => data.AsString();

        private static byte[] BoolToBytes(bool val) => val ? (new byte[1] { 1 }) : (new byte[1] { 0 });

        private static BigInteger BytesToBigInteger(byte[] data) => data.AsBigInteger();

        [DisplayName("GenerateHash")]
        private static string GenerateSHA256(int stake, int nonce)
        {
            //Store int values to retrieve it in bytes
            //NEOCompiler doesn't recognize System.BitConverter.GetBytes
            Storage.Put(Storage.CurrentContext, "bytes-stake", stake);
            Storage.Put(Storage.CurrentContext, "bytes-nonce", nonce);
            //byte[] input = AppendByteArrays(Storage.Get(Storage.CurrentContext, "bytes-stake"), Storage.Get(Storage.CurrentContext, "bytes-nonce"));
            byte[] input = Neo.SmartContract.Framework.Helper.Concat(Storage.Get(Storage.CurrentContext, "bytes-stake"), Storage.Get(Storage.CurrentContext, "bytes-nonce"));
            //Remove from storage
            Storage.Delete(Storage.CurrentContext, "bytes-stake");
            Storage.Delete(Storage.CurrentContext, "bytes-nonce");
            byte[] hash = Sha256(input);
            //return GetStringFromHash(hash);
            return hash.AsString();
        }

        //private static string GetStringFromHash(byte[] hash)
        //{
        //    StringBuilder str = new StringBuilder();
        //    foreach (byte b in hash)
        //    {
        //        str.Append(b.ToString("X2"));
        //    }
        //    return str.ToString();
        //}

        //private static byte[] AppendByteArrays(byte[] array1, byte[] array2)
        //{
        //    byte[] array3 = new byte[array1.Length + array2.Length];

        //    //append first array
        //    for (int i = 0; i < array1.Length; i++)
        //    {
        //        array3[i] = array1[i];
        //    }

        //    //append 2nd array
        //    for (int i = array1.Length; i < array3.Length; i++)
        //    {
        //        array3[i] = array2[i - array1.Length];
        //    }

        //    return array3;
        //}

        //private static byte[] Serialize(object obj)
        //{
        //    if (obj == null) return null;

        //    BinaryFormatter bf = new BinaryFormatter();
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        bf.Serialize(ms, obj);
        //        return ms.ToArray();
        //    }
        //}

        //private static Object Deserialize(byte[] bytes)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        BinaryFormatter bf = new BinaryFormatter();
        //        ms.Write(bytes, 0, bytes.Length);
        //        ms.Seek(0, SeekOrigin.Begin);
        //        return bf.Deserialize(ms);
        //    }
        //}
    }

    //[Serializable()]
    [Serializable]
    public class Auction
    {
        public string secret { get; set; }
        public uint startTime { get; set; }
        public uint endOfBidding { get; set; }
        public uint endOfRevealing { get; set; }
        public uint endOfResulting { get; set; }

        public BigInteger reservePrice { get; set; }
        public BigInteger highestBid { get; set; }
        public BigInteger secondBid { get; set; }
        public byte[] higherBidder { get; set; }
        public bool hasResulted { get; set; }

        public List<Bidder> bidders;

        public Auction(string secret, uint startTime, int durationBidding, int durationRevealing, int durationResulting, BigInteger reservePrice, byte[] higherBidder)
        {
            this.secret = secret;
            this.startTime = startTime;
            this.endOfBidding = startTime + (uint)durationBidding;
            this.endOfRevealing = startTime + (uint)durationBidding + (uint)durationRevealing;
            this.endOfResulting = startTime + (uint)durationBidding + (uint)durationRevealing + (uint)durationResulting;

            this.reservePrice = reservePrice;
            this.highestBid = reservePrice;
            this.secondBid = reservePrice;

            this.higherBidder = higherBidder;
            bidders = new List<Bidder>();
            hasResulted = false;
        }

        public void ConfirmReveal(byte[] address)
        {
            bidders[GetBidderIndex(address)].hasRevealed = true;
        }

        public void SetBiderHash(byte[] address, string hash)
        {
            bidders[GetBidderIndex(address)].hash = hash;
        }

        public void SetBiderStake(byte[] address, int stake)
        {
            bidders[GetBidderIndex(address)].stake = stake;
        }

        public int GetBiderStake(byte[] address)
        {
            return bidders[GetBidderIndex(address)].stake;
        }

        public bool AnnounceBidder(byte[] address)
        {
            if (GetBidderIndex(address) != -1) return false;
            bidders.Add(new Bidder(address));
            return true;
        }

        private int GetBidderIndex(byte[] address)
        {
            for (int i = 0; i < bidders.Count; i++)
            {
                if (CompareByteArrays(bidders[i].address, address)) return i;
            }

            return -1;
        }

        public string GetBidderHash(byte[] address)
        {
            for (int i = 0; i < bidders.Count; i++)
            {
                if (CompareByteArrays(bidders[i].address, address)) return bidders[i].hash;
            }

            return null;
        }

        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length) return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }
    }

    //[Serializable()]
    [Serializable]
    public class Bidder
    {
        public byte[] address { get; set; }
        public int stake { get; set; }
        public string hash { get; set; }
        public bool hasRevealed { get; set; }
        public bool hasAnnounced { get; set; }


        public Bidder(byte[] address)
        {
            this.address = address;
            this.hasAnnounced = true;
        }
    }
}
