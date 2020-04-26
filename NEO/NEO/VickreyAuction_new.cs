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
    public class VickreyAuction_new : SmartContract
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

                if (method == "Deploy") return Deploy();

                if (method == "TotalSupply") return TotalSupply();

                if (method == "Init") return Init((string)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4]);

                if (method == "Announce") return Announce((byte[])args[0]);

                if (method == "Bid") return Bid((byte[])args[0], (string)args[1]);

                if (method == "Result") return Result((byte[])args[0]);

                if (method == "End") return End();

                if (method == "Reveal") return Reveal((byte[])args[0], (int)args[1], (int)args[2]);

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
            //Auction auction = new Auction(secret, Runtime.Time, durationBidding, durationReveal, durationResulting, reservePrice, Owner);
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
            //Storage.Put(Storage.CurrentContext, "auction", Helper.Serialize(auction));

            Storage.Put("secret", secret);
            Storage.Put("reservePrice", reservePrice);
            Storage.Put("endOfBidding", Runtime.Time + (uint)durationBidding);
            Storage.Put("endOfRevealing", durationBidding + durationReveal);
            Storage.Put("endOfResulting", durationReveal + durationResulting);
            Storage.Put("highestBid", reservePrice);
            Storage.Put("secondBid", reservePrice);
            Storage.Put("higherBidder", Owner);
            Storage.Put("hasResulted", 0);

            //Storage.Put("auction", Helper.Serialize(auction));
            return true;
        }

        private static bool Announce(byte[] address)
        {
            if (Runtime.CheckWitness(Owner)) return false;
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            //Auction auction = (Auction)Helper.Deserialize(Storage.Get("auction"));
            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (now >= endOfBidding) return false;

            //if (!auction.AnnounceBidder(address)) return false;
            StorageMap announceBidder = Storage.CurrentContext.CreateMap(nameof(announceBidder));
            if ((int)BytesToBigInteger(announceBidder.Get(address)) == 1) return false;
            announceBidder.Put(address, 1);

            Transferred(null, address, 1000);
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
            Storage.Put("totalSupply", BytesToBigInteger(Storage.Get("totalSupply")) + 1000);
            return true;
        }

        private static bool Bid(byte[] bidderAddress, string hash)
        {
            if (Runtime.CheckWitness(Owner)) return false;

            //Deserialize auction
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();

            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (Runtime.Time >= endOfBidding) return false;

            //Store bidder's info
            //auction.SetBiderHash(bidderAddress, hash);
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());

            StorageMap hashAdd = Storage.CurrentContext.CreateMap(nameof(hashAdd));
            hashAdd.Put(bidderAddress, hash);

            return true;
        }

        private static bool Reveal(byte[] senderAddress, int stake, int nonce)
        {
            //Get auction
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            uint endOfRevealing = (uint)BytesToBigInteger(Storage.Get("endOfRevealing"));
            if (now < endOfBidding || now >= endOfRevealing) return false;

            //Get bidder's hash
            //string hash = auction.GetBidderHash(senderAddress);
            StorageMap hashAdd = Storage.CurrentContext.CreateMap(nameof(hashAdd));
            string hash = BytesToString(hashAdd.Get(senderAddress));
            if (hash == null) return false;

            //Compute hash and compare
            string generatedHash = GenerateSHA256(stake, nonce);
            if (hash != generatedHash) return false;

            //auction.SetBiderStake(senderAddress, stake);
            StorageMap biderStake = Storage.CurrentContext.CreateMap(nameof(biderStake));
            biderStake.Put(senderAddress, stake);

            //Confirm reveal of bidder
            //auction.ConfirmReveal(senderAddress);
            StorageMap confirmReveal = Storage.CurrentContext.CreateMap(nameof(confirmReveal));
            confirmReveal.Put(senderAddress, 1);
            Transferred(senderAddress, null, stake);

            int highestBid = (int)BytesToBigInteger(Storage.Get("highestBid"));
            int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));
            if (stake > highestBid)
            {
                //auction.secondBid = auction.highestBid;
                //auction.highestBid = stake;
                //auction.higherBidder = senderAddress;
                Storage.Put("secondBid", highestBid);
                Storage.Put("highestBid", stake);
                Storage.Put("higherBidder", senderAddress);
            }
            else if (stake > secondBid)
            {
                //auction.secondBid = stake;
                Storage.Put("secondBid", stake);
            }
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());

            return true;
        }

        private static string Result(byte[] senderAddress)
        {
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            uint now = Runtime.Time;
            uint endOfResulting = (uint)BytesToBigInteger(Storage.Get("endOfResulting"));
            uint endOfRevealing = (uint)BytesToBigInteger(Storage.Get("endOfRevealing"));
            if (now < endOfRevealing || now >= endOfResulting) return "wait";
            byte[] higherBidder = Storage.Get("higherBidder");
            if (Runtime.CheckWitness(higherBidder))
            {
                //create variable to know if highBidder has called result
                //auction.hasResulted = true;
                //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
                Storage.Put("hasResulted", 1);
                int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));
                Transferred(higherBidder, Owner, secondBid);
                string secret = BytesToString(Storage.Get("secret"));
                return secret;
            }
            else
            {
                StorageMap biderStake = Storage.CurrentContext.CreateMap(nameof(biderStake));
                //Transferred(null, senderAddress, auction.GetBiderStake(senderAddress));
                Transferred(null, senderAddress, BytesToBigInteger(biderStake.Get(senderAddress)));
            }
            return "true";
        }

        private static bool End()
        {
            if (!Runtime.CheckWitness(Owner)) return false;
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            uint endOfResulting = (uint)BytesToBigInteger(Storage.Get("endOfResulting"));
            if (Runtime.Time < endOfResulting) return false;
            
            int hasResulted = (int)BytesToBigInteger(Storage.Get("hasResulted"));
            if (hasResulted == 0)
            {
                byte[] higherBidder = Storage.Get("higherBidder");
                int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));
                Transferred(higherBidder, Owner, secondBid);
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
    }

}