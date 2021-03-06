using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NEO
{
    public class VickreyAuction : SmartContract
    {
        private static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash(); //Owner Address
        private static readonly BigInteger TotalSupplyValue = 1000;     //1000 VNEO

        /// <summary>
        /// Return the token's name
        /// </summary>
        /// <returns>string</returns>
        [DisplayName("Name")]
        public static string Name() => "VNEO"; //name of the token

        /// <summary>
        /// Return the token's symbol
        /// </summary>
        /// <returns>string</returns>
        [DisplayName("Symbol")]
        public static string Symbol() => "VNEO"; //symbol of the token

        /// <summary>
        /// Return the token's number of decimals
        /// </summary>
        /// <returns>byte</returns>
        [DisplayName("Decimals")]
        public static byte Decimals() => 0;

        /// <summary>
        /// Returns the Supported standards
        /// </summary>
        /// <returns>string</returns>
        [DisplayName("SupportedStandards")]
       public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        /// <summary>
        /// Transfer token
        /// </summary>
        [DisplayName("Transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        /// <summary>
        /// Smart contract entry point
        /// </summary>
        /// <param name="method">method to call</param>
        /// <param name="args">array with required params</param>
        /// <returns>object</returns>
        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(Owner);
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "BalanceOf") return BalanceOf((byte[])args[0]);

                if (method == "TotalSupply") return TotalSupply();

                if (method == "Deploy") return Deploy();

                if (method == "Init") return Init((string)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4]);

                if (method == "Announce") return Announce((byte[])args[0]);

                if (method == "Bid") return Bid((byte[])args[0], (byte[])args[1]);

                if (method == "Reveal") return Reveal((byte[])args[0], (int)args[1], (int)args[2]);

                if (method == "Result") return Result((byte[])args[0]);

                if (method == "End") return End();

                if (method == "GenerateHash") return GenerateHash((int)args[0], (int)args[1]);

                if (method == "CompareHash") return CompareHash((int)args[0], (int)args[1], (byte[])args[2]);
            }
            return false;
        }

        /// <summary>
        /// Returns the token balance of the account
        /// </summary>
        /// <param name="account">20-byte address</param>
        /// <returns>BigInteger</returns>
        [DisplayName("BalanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
                throw new InvalidOperationException("The parameter account SHOULD be 20-byte addresses.");
            if (!Runtime.CheckWitness(account))
                throw new InvalidOperationException("Only the account can see his balance"); ;
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            return asset.Get(account).AsBigInteger();
        }

        /// <summary>
        /// Returns the total token supply deployed in the system
        /// </summary>
        /// <returns>BigInteger</returns>
        [DisplayName("TotalSupply")]
        public static BigInteger TotalSupply()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return contract.Get("totalSupply").AsBigInteger();
        }

        /// <summary>
        /// Deploy the token in the system and set the total supply value
        /// </summary>
        /// <returns>bool</returns>
        [DisplayName("Deploy")]
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

        /// <summary>
        /// Initialize new Vickrey Auction
        /// </summary>
        /// <param name="secret">secret auctioned</param>
        /// <param name="reservePrice">minimum price</param>
        /// <param name="durationBidding">duration of bidding period in seconds</param>
        /// <param name="durationReveal">duration of reveal period in seconds</param>
        /// <param name="durationResulting"duration of resulting period in seconds></param>
        /// <returns>string</returns>
        [DisplayName("Init")]
        private static string Init(string secret, BigInteger reservePrice, int durationBidding, int durationReveal, int durationResulting)
        {
            if (!Runtime.CheckWitness(Owner)) return "not Owner";

            Storage.Put("secret", secret);
            Storage.Put("endOfBidding", Runtime.Time + durationBidding);
            Storage.Put("endOfRevealing", Runtime.Time + durationBidding + durationReveal);
            Storage.Put("endOfResulting", Runtime.Time + durationBidding + durationReveal + durationResulting);
            Storage.Put("highestBid", reservePrice);
            Storage.Put("secondBid", reservePrice);
            Storage.Put("higherBidder", Owner);
            Storage.Put("bidder", 0);
            Storage.Put("hasResulted", 0);

            return "success";
        }

        /// <summary>
        /// Announce the intention of bidding
        /// </summary>
        /// <param name="address">bidder's address</param>
        /// <returns>string</returns>
        [DisplayName("Announce")]
        private static string Announce(byte[] address)
        {
            if (Runtime.CheckWitness(Owner)) return "Owner cannot Annonce";
            if (!Runtime.CheckWitness(address)) return "address does not match";

            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (now >= endOfBidding) return "not in time";

            StorageMap announceBidder = Storage.CurrentContext.CreateMap(nameof(announceBidder));
            if ((int)BytesToBigInteger(announceBidder.Get(address)) == 1) return "can annonce only once";
            announceBidder.Put(address, 1);

            Storage.Put("totalSupply", BytesToBigInteger(Storage.Get("totalSupply")) + 1000);

            //Issue NEP-5 asset
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var amount = asset.Get(address).AsBigInteger();
            asset.Put(address, amount + 1000);
            Transferred(null, address, 1000);

            return "success";
        }

        /// <summary>
        /// Bid
        /// </summary>
        /// <param name="bidderAddress">bidder's address</param>
        /// <param name="hash">hash create from stake and nonce</param>
        /// <returns>string</returns>
        [DisplayName("Bid")]
        private static string Bid(byte[] bidderAddress, byte[] hash)
        {
            if (Runtime.CheckWitness(Owner)) return "Owner cannot Bid";
            if (!Runtime.CheckWitness(bidderAddress)) return "address does not match";

            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            if (Runtime.Time >= endOfBidding) return "not in time";

            StorageMap bidded = Storage.CurrentContext.CreateMap(nameof(bidded));
            if ((int)BytesToBigInteger(bidded.Get(bidderAddress)) == 1) return "can only bid once";
            bidded.Put(bidderAddress, 1);

            StorageMap hashAdd = Storage.CurrentContext.CreateMap(nameof(hashAdd));
            hashAdd.Put(bidderAddress, hash);

            return "success";
        }

        /// <summary>
        /// Reveal the bid previously done
        /// </summary>
        /// <param name="senderAddress">bidder's address</param>
        /// <param name="stake">amount bidden</param>
        /// <param name="nonce">nonce</param>
        /// <returns>string</returns>
        [DisplayName("Reveal")]
        private static string Reveal(byte[] senderAddress, int stake, int nonce)
        {
            if (Runtime.CheckWitness(Owner)) return "Owner cannot Reveal";
            if (!Runtime.CheckWitness(senderAddress)) return "address does not match";

            uint now = Runtime.Time;
            uint endOfBidding = (uint)BytesToBigInteger(Storage.Get("endOfBidding"));
            uint endOfRevealing = (uint)BytesToBigInteger(Storage.Get("endOfRevealing"));
            if (now < endOfBidding || now >= endOfRevealing) return "not in time";      //pass if 0 || 0

            StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));
            if ((int)BytesToBigInteger(revealed.Get(senderAddress)) == 1) return "can only reveal once";
            revealed.Put(senderAddress, 1);

            //Get bidder's hash
            StorageMap hashAdd = Storage.CurrentContext.CreateMap(nameof(hashAdd));
            byte[] hash = hashAdd.Get(senderAddress);
            if (hash == null) return "hash is null";

            //Compute hash and compare
            if (!CompareHash(stake, nonce, hash)) return "hash does not match";

            //transfer NEP-5 asset
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var amount = asset.Get(senderAddress).AsBigInteger();
            if (amount >= stake)
            {
                asset.Put(senderAddress, amount - stake);
            }
            else
            {
                stake = (int)amount;
                asset.Put(senderAddress, 0);
            }
            Transferred(senderAddress, null, stake);

            // store the stake related to the address
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
            return "success";
        }

        /// <summary>
        /// Get the result of the auction depending on the address
        /// </summary>
        /// <param name="senderAddress">bidder's address</param>
        /// <returns>string</returns>
        [DisplayName("Result")]
        private static string Result(byte[] senderAddress)
        {
            if (Runtime.CheckWitness(Owner)) return "Owner cannot Result";
            if (!Runtime.CheckWitness(senderAddress)) return "address does not match";

            uint now = Runtime.Time;
            uint endOfResulting = (uint)BytesToBigInteger(Storage.Get("endOfResulting"));
            uint endOfRevealing = (uint)BytesToBigInteger(Storage.Get("endOfRevealing"));
            if (now < endOfRevealing || now >= endOfResulting) return "not in time";

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
                asset.Put(higherBidder, amount2 + highestBid - secondBid);
                Runtime.Notify("4");
                Transferred(null, higherBidder, highestBid - secondBid);

                string secret = BytesToString(Storage.Get("secret"));
                return secret;
            }
            else
            {
                StorageMap biderStake = Storage.CurrentContext.CreateMap(nameof(biderStake));
                int value = (int)BytesToBigInteger(biderStake.Get(senderAddress));
                var amount3 = asset.Get(senderAddress).AsBigInteger();
                asset.Put(senderAddress, amount3 + value);
                Transferred(null, senderAddress, value);
                return "refund";
            }
        }

        /// <summary>
        /// Call end of the auction by the owner
        /// If highest bidder hasn't called the result method, withdraw amount of token
        /// </summary>
        /// <returns>string</returns>
        [DisplayName("End")]
        private static string End()
        {
            if (!Runtime.CheckWitness(Owner)) return "only the Owner can End";

            uint endOfResulting = (uint)BytesToBigInteger(Storage.Get("endOfResulting"));
            if (Runtime.Time < endOfResulting) return "not in time";

            if ((int)BytesToBigInteger(Storage.Get("bidder")) == 0) return "no bidder";

            if ((int)BytesToBigInteger(Storage.Get("hasEnded")) == 1) return "can only End once";
            Storage.Put("hasEnded", 1);

            //if the higherBidder has not resulted
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
            return "success";
        }

        /// <summary>
        /// Generate the hash with stake and nonce
        /// </summary>
        /// <param name="stake">amount bidden</param>
        /// <param name="nonce">nonce</param>
        /// <returns>byte[]</returns>
        [DisplayName("GenerateHash")]
        private static byte[] GenerateHash(int stake, int nonce)
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

        /// <summary>
        /// Compare hash in params with stake and nonce
        /// </summary>
        /// <param name="stake">amount</param>
        /// <param name="nonce">nonce</param>
        /// <param name="hash">previously computed hash</param>
        /// <returns>bool</returns>
        private static bool CompareHash(int stake, int nonce, byte[] hash)
        {
            //Compute hash and compare
            byte[] generatedHash = GenerateHash(stake, nonce);
            if (hash != generatedHash) return false;

            return true;
        }

        /// <summary>
        /// Convert byte array to string
        /// </summary>
        /// <param name="data"></param>
        /// <returns>string</returns>
        private static string BytesToString(byte[] data) => data.AsString();


        /// <summary>
        /// Convert byte array to BigInteger
        /// </summary>
        /// <param name="data"></param>
        /// <returns>BigInteger</returns>
        private static BigInteger BytesToBigInteger(byte[] data) => data.AsBigInteger();

    }

}
