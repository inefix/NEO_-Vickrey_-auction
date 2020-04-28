using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NEO
{
    public class VickreyAuction_new : SmartContract
    {
        private static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash(); //Owner Address
        private static readonly BigInteger TotalSupplyValue = 1000;     //1000 VNEO

        [DisplayName("name")]
        public static string Name() => "VNEO"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "VNEO"; //symbol of the token

        [DisplayName("decimals")]
        public static byte Decimals() => 0;

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

                if (method == "Bid") return Bid((byte[])args[0], (byte[])args[1]);

                if (method == "Result") return Result((byte[])args[0]);

                if (method == "End") return End();

                if (method == "Reveal") return Reveal((byte[])args[0], (int)args[1], (int)args[2]);

                if (method == "GenerateHash") return GenerateSHA256((int)args[0], (int)args[1]);

                if (method == "CompareHash") return CompareHash((int)args[0], (int)args[1], (byte[])args[2]);
            }
            return false;
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
                throw new InvalidOperationException("The parameter account SHOULD be 20-byte addresses.");
            if (!Runtime.CheckWitness(account))
                throw new InvalidOperationException("Only the account can see his balance"); ;
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
            Runtime.Notify("0");
            if (!Runtime.CheckWitness(Owner)) return false;
            Runtime.Notify("1");
            //Auction
            //Auction auction = new Auction(secret, Runtime.Time, durationBidding, durationReveal, durationResulting, reservePrice, Owner);
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
            //Storage.Put(Storage.CurrentContext, "auction", Helper.Serialize(auction));

            Storage.Put("secret", secret);
            //Storage.Put("reservePrice", reservePrice);
            Storage.Put("endOfBidding", Runtime.Time + durationBidding);
            Storage.Put("endOfRevealing", Runtime.Time + durationBidding + durationReveal);
            Storage.Put("endOfResulting", Runtime.Time + durationBidding + durationReveal + durationResulting);
            Storage.Put("highestBid", reservePrice);
            Storage.Put("secondBid", reservePrice);
            Storage.Put("higherBidder", Owner);
            Storage.Put("bidder", 0);
            Storage.Put("hasResulted", 0);

            //Storage.Put("auction", Helper.Serialize(auction));
            return true;
        }

        private static bool Announce(byte[] address)
        {
            if (Runtime.CheckWitness(Owner)) return false;
            if (!Runtime.CheckWitness(address)) return false;
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            //Auction auction = (Auction)Helper.Deserialize(Storage.Get("auction"));
            Runtime.Notify("0");
            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (now >= endOfBidding) return false;
            Runtime.Notify("1");
            //if (!auction.AnnounceBidder(address)) return false;
            StorageMap announceBidder = Storage.CurrentContext.CreateMap(nameof(announceBidder));
            if ((int)BytesToBigInteger(announceBidder.Get(address)) == 1) return false;
            announceBidder.Put(address, 1);
            Runtime.Notify("2");
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
            Storage.Put("totalSupply", BytesToBigInteger(Storage.Get("totalSupply")) + 1000);
            Runtime.Notify("3");

            //Issue NEP-5 asset
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var amount = asset.Get(address).AsBigInteger(); //0.1
            asset.Put(address, amount + 1000); //1
            Runtime.Notify("4");
            Transferred(null, address, 1000);

            return true;
        }

        private static bool Bid(byte[] bidderAddress, byte[] hash)
        {
            if (Runtime.CheckWitness(Owner)) return false;
            if (!Runtime.CheckWitness(bidderAddress)) return false;

            //Deserialize auction
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();

            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (Runtime.Time >= endOfBidding) return false;

            StorageMap bidded = Storage.CurrentContext.CreateMap(nameof(bidded));
            if ((int)BytesToBigInteger(bidded.Get(bidderAddress)) == 1) return false;
            bidded.Put(bidderAddress, 1);

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
            Runtime.Notify("-1");
            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            uint endOfRevealing = (uint)BytesToBigInteger(Storage.Get("endOfRevealing"));
            Runtime.Notify("0");
            if (now < endOfBidding || now >= endOfRevealing) return false;      //pass if 0 || 0
            Runtime.Notify("1");

            StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));
            if ((int)BytesToBigInteger(revealed.Get(senderAddress)) == 1) return false;
            revealed.Put(senderAddress, 1);

            //Get bidder's hash
            //string hash = auction.GetBidderHash(senderAddress);
            StorageMap hashAdd = Storage.CurrentContext.CreateMap(nameof(hashAdd));
            byte[] hash = hashAdd.Get(senderAddress);
            if (hash == null) return false;
            Runtime.Notify("3");

            //Compute hash and compare
            //byte[] generatedHash = GenerateSHA256(stake, nonce);
            //if (hash != generatedHash) return false;
            if (!CompareHash(stake, nonce, hash)) return false;
            Runtime.Notify("4");

            //transfer NEP-5 asset
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var amount = asset.Get(senderAddress).AsBigInteger();
            if (amount >= stake)
            {
                asset.Put(senderAddress, amount - stake);
            } else
            {
                stake = (int)amount;
                asset.Put(senderAddress, 0);
            }
            Transferred(senderAddress, null, stake);
            Runtime.Notify("2");

            //auction.SetBiderStake(senderAddress, stake);
            StorageMap biderStake = Storage.CurrentContext.CreateMap(nameof(biderStake));
            biderStake.Put(senderAddress, stake);

            Runtime.Notify("5");

            int highestBid = (int)BytesToBigInteger(Storage.Get("highestBid"));
            int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));
            Runtime.Notify(stake);
            if (stake > highestBid)
            {
                //auction.secondBid = auction.highestBid;
                //auction.highestBid = stake;
                //auction.higherBidder = senderAddress;
                Storage.Put("secondBid", highestBid);
                Storage.Put("highestBid", stake);
                Storage.Put("higherBidder", senderAddress);
                Runtime.Notify("10");
                Storage.Put("bidder", 1);
                Runtime.Notify("6");
            }
            else if (stake > secondBid)
            {
                //auction.secondBid = stake;
                Storage.Put("secondBid", stake);
                Runtime.Notify("9");
                Storage.Put("bidder", 1);
                Runtime.Notify("7");
            }
            //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
            Runtime.Notify("8");
            return true;
        }

        private static string Result(byte[] senderAddress)
        {
            //Auction auction = (Auction)Storage.Get(Storage.CurrentContext, "auction").Deserialize();
            uint now = Runtime.Time;
            uint endOfResulting = (uint)BytesToBigInteger(Storage.Get("endOfResulting"));
            uint endOfRevealing = (uint)BytesToBigInteger(Storage.Get("endOfRevealing"));
            if (now < endOfRevealing || now >= endOfResulting) return "wait";

            StorageMap resulted = Storage.CurrentContext.CreateMap(nameof(resulted));
            if ((int)BytesToBigInteger(resulted.Get(senderAddress)) == 1) return "already resulted";
            resulted.Put(senderAddress, 1);

            byte[] higherBidder = Storage.Get("higherBidder");
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            if (Runtime.CheckWitness(higherBidder))
            {
                //create variable to know if highBidder has called result
                //auction.hasResulted = true;
                //Storage.Put(Storage.CurrentContext, "auction", auction.Serialize());
                Storage.Put("hasResulted", 1);
                int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));

                //transfer to the Owner
                var amount = asset.Get(Owner).AsBigInteger();
                asset.Put(Owner, amount + secondBid);
                Runtime.Notify("4");
                Transferred(null, Owner, secondBid);

                //transfer of the difference to the higherBidder
                int highestBid = (int)BytesToBigInteger(Storage.Get("highestBid"));
                asset.Put(Owner, amount + highestBid - secondBid); //1
                Runtime.Notify("4");
                Transferred(null, higherBidder, amount + highestBid - secondBid);

                string secret = BytesToString(Storage.Get("secret"));
                return secret;
            }
            else
            {
                StorageMap biderStake = Storage.CurrentContext.CreateMap(nameof(biderStake));
                //Transferred(null, senderAddress, auction.GetBiderStake(senderAddress));

                var amount = asset.Get(senderAddress).AsBigInteger();
                asset.Put(Owner, amount + BytesToBigInteger(biderStake.Get(senderAddress)));
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

            if ((int)BytesToBigInteger(Storage.Get("bidder")) == 1) return false;

            if ((int)BytesToBigInteger(Storage.Get("hasEnded")) == 1) return false;
            Storage.Put("hasEnded", 1);

            int hasResulted = (int)BytesToBigInteger(Storage.Get("hasResulted"));
            if (hasResulted == 0)
            {
                //byte[] higherBidder = Storage.Get("higherBidder");
                int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));

                //transfer to the Owner
                StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
                var amount = asset.Get(Owner).AsBigInteger();
                asset.Put(Owner, amount + secondBid);
                Transferred(null, Owner, secondBid);

                //transfer of the difference to the higherBidder
                int highestBid = (int)BytesToBigInteger(Storage.Get("highestBid"));
                byte[] higherBidder = Storage.Get("higherBidder");
                asset.Put(Owner, amount + highestBid - secondBid);
                Runtime.Notify("4");
                Transferred(null, higherBidder, amount + highestBid - secondBid);
            }
            return true;
        }

        private static string BytesToString(byte[] data) => data.AsString();

        private static byte[] BoolToBytes(bool val) => val ? (new byte[1] { 1 }) : (new byte[1] { 0 });

        private static BigInteger BytesToBigInteger(byte[] data) => data.AsBigInteger();

        [DisplayName("GenerateHash")]
        private static byte[] GenerateSHA256(int stake, int nonce)
        {
            //Store int values to retrieve it in bytes
            //NEOCompiler doesn't recognize System.BitConverter.GetBytes
            //Runtime.Notify("0");
            Storage.Put("bytes-stake", stake);
            Storage.Put("bytes-nonce", nonce);
            //Runtime.Notify("1");
            //byte[] input = AppendByteArrays(Storage.Get(Storage.CurrentContext, "bytes-stake"), Storage.Get(Storage.CurrentContext, "bytes-nonce"));
            byte[] input = Neo.SmartContract.Framework.Helper.Concat(Storage.Get("bytes-stake"), Storage.Get("bytes-nonce"));
            //Remove from storage
            //Runtime.Notify("2");
            Storage.Delete("bytes-stake");
            Storage.Delete("bytes-nonce");
            //Runtime.Notify("3");
            byte[] hash = Sha256(input);
            //Runtime.Notify("4");
            //return GetStringFromHash(hash);
            //return hash.AsString();
            return hash;
        }

        private static bool CompareHash(int stake, int nonce, byte[] hash)
        {
            //Compute hash and compare
            //Runtime.Notify("0");
            byte[] generatedHash = GenerateSHA256(stake, nonce);
            //Runtime.Notify("1");
            if (hash != generatedHash) return false;
            //Runtime.Notify("2");

            return true;
        }

    }

}
