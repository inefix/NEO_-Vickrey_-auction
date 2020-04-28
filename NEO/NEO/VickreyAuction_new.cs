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
            if (!Runtime.CheckWitness(Owner)) return false;

            Storage.Put("secret", secret);
            Storage.Put("endOfBidding", Runtime.Time + durationBidding);
            Storage.Put("endOfRevealing", Runtime.Time + durationBidding + durationReveal);
            Storage.Put("endOfResulting", Runtime.Time + durationBidding + durationReveal + durationResulting);
            Storage.Put("highestBid", reservePrice);
            Storage.Put("secondBid", reservePrice);
            Storage.Put("higherBidder", Owner);
            Storage.Put("bidder", 0);
            Storage.Put("hasResulted", 0);

            return true;
        }

        private static bool Announce(byte[] address)
        {
            if (Runtime.CheckWitness(Owner)) return false;
            if (!Runtime.CheckWitness(address)) return false;

            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (now >= endOfBidding) return false;

            StorageMap announceBidder = Storage.CurrentContext.CreateMap(nameof(announceBidder));
            if ((int)BytesToBigInteger(announceBidder.Get(address)) == 1) return false;
            announceBidder.Put(address, 1);

            Storage.Put("totalSupply", BytesToBigInteger(Storage.Get("totalSupply")) + 1000);

            //Issue NEP-5 asset
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var amount = asset.Get(address).AsBigInteger(); //0.1
            asset.Put(address, amount + 1000); //1
            Transferred(null, address, 1000);

            return true;
        }

        private static bool Bid(byte[] bidderAddress, byte[] hash)
        {
            if (Runtime.CheckWitness(Owner)) return false;
            if (!Runtime.CheckWitness(bidderAddress)) return false;

            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (Runtime.Time >= endOfBidding) return false;

            StorageMap bidded = Storage.CurrentContext.CreateMap(nameof(bidded));
            if ((int)BytesToBigInteger(bidded.Get(bidderAddress)) == 1) return false;
            bidded.Put(bidderAddress, 1);

            StorageMap hashAdd = Storage.CurrentContext.CreateMap(nameof(hashAdd));
            hashAdd.Put(bidderAddress, hash);

            return true;
        }

        private static bool Reveal(byte[] senderAddress, int stake, int nonce)
        {
            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            uint endOfRevealing = (uint)BytesToBigInteger(Storage.Get("endOfRevealing"));
            if (now < endOfBidding || now >= endOfRevealing) return false;      //pass if 0 || 0

            StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));
            if ((int)BytesToBigInteger(revealed.Get(senderAddress)) == 1) return false;
            revealed.Put(senderAddress, 1);

            //Get bidder's hash
            StorageMap hashAdd = Storage.CurrentContext.CreateMap(nameof(hashAdd));
            byte[] hash = hashAdd.Get(senderAddress);
            if (hash == null) return false;
            Runtime.Notify("3");

            //Compute hash and compare
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

            StorageMap biderStake = Storage.CurrentContext.CreateMap(nameof(biderStake));
            biderStake.Put(senderAddress, stake);

            int highestBid = (int)BytesToBigInteger(Storage.Get("highestBid"));
            int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));
            if (stake > highestBid)
            {
                Storage.Put("secondBid", highestBid);
                Storage.Put("highestBid", stake);
                Storage.Put("higherBidder", senderAddress);
                Storage.Put("bidder", 1);
            }
            else if (stake > secondBid)
            {
                Storage.Put("secondBid", stake);
                Storage.Put("bidder", 1);
            }
            return true;
        }

        private static string Result(byte[] senderAddress)
        {
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
                Storage.Put("hasResulted", 1);
                int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));

                //transfer to the Owner
                var amount = asset.Get(Owner).AsBigInteger();
                asset.Put(Owner, amount + secondBid);
                Runtime.Notify("4");
                Transferred(null, Owner, secondBid);

                //transfer of the difference to the higherBidder
                int highestBid = (int)BytesToBigInteger(Storage.Get("highestBid"));
                var amount2 = asset.Get(higherBidder).AsBigInteger();
                asset.Put(higherBidder, amount2 + highestBid - secondBid); //1
                Runtime.Notify("4");
                Transferred(null, higherBidder, highestBid - secondBid);

                string secret = BytesToString(Storage.Get("secret"));
                return secret;
            }
            else
            {
                StorageMap biderStake = Storage.CurrentContext.CreateMap(nameof(biderStake));
                var amount = asset.Get(senderAddress).AsBigInteger();
                asset.Put(Owner, amount + BytesToBigInteger(biderStake.Get(senderAddress)));
                Transferred(null, senderAddress, BytesToBigInteger(biderStake.Get(senderAddress)));
            }
            return "true";
        }

        private static bool End()
        {
            if (!Runtime.CheckWitness(Owner)) return false;

            uint endOfResulting = (uint)BytesToBigInteger(Storage.Get("endOfResulting"));
            if (Runtime.Time < endOfResulting) return false;

            if ((int)BytesToBigInteger(Storage.Get("bidder")) == 0) return false;

            if ((int)BytesToBigInteger(Storage.Get("hasEnded")) == 1) return false;
            Storage.Put("hasEnded", 1);

            int hasResulted = (int)BytesToBigInteger(Storage.Get("hasResulted"));
            if (hasResulted == 0)
            {
                int secondBid = (int)BytesToBigInteger(Storage.Get("secondBid"));

                //transfer to the Owner
                StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
                var amount = asset.Get(Owner).AsBigInteger();
                asset.Put(Owner, amount + secondBid);
                Transferred(null, Owner, secondBid);

                //transfer of the difference to the higherBidder
                int highestBid = (int)BytesToBigInteger(Storage.Get("highestBid"));
                byte[] higherBidder = Storage.Get("higherBidder");
                var amount2 = asset.Get(higherBidder).AsBigInteger();
                asset.Put(higherBidder, amount2 + highestBid - secondBid);
                Transferred(null, higherBidder, highestBid - secondBid);
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
            Storage.Put("bytes-stake", stake);
            Storage.Put("bytes-nonce", nonce);

            byte[] input = Neo.SmartContract.Framework.Helper.Concat(Storage.Get("bytes-stake"), Storage.Get("bytes-nonce"));

            //Remove from storage
            Storage.Delete("bytes-stake");
            Storage.Delete("bytes-nonce");

            byte[] hash = Sha256(input);

            return hash;
        }

        private static bool CompareHash(int stake, int nonce, byte[] hash)
        {
            //Compute hash and compare
            byte[] generatedHash = GenerateSHA256(stake, nonce);
            if (hash != generatedHash) return false;

            return true;
        }

    }

}
