using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace NEO
{
    public class VickreyAuction : SmartContract
    {
        private static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash(); //Owner Address
        private static readonly BigInteger TotalSupplyValue = 10000000000000000;

        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private static readonly byte[] gas_asset_id = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };

        [DisplayName("name")]
        public static string Name() => "VA"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "VA"; //symbol of the token

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

                if (method == "Decimals") return Decimals();

                if (method == "Deploy") return Deploy();

                if (method == "Name") return Name();

                if (method == "Symbol") return Symbol();

                if (method == "SupportedStandards") return SupportedStandards();

                if (method == "TotalSupply") return TotalSupply();

                if (method == "Transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);

                if (method == "Init") return Init((int)args[0], (int)args[1], (int)args[2], (int)args[3]);

                //if (method == "Bid") return Bid((string)args[0]);

                if (method == "Claim") return Claim();

                if (method == "Reveal") return Reveal((int)args[0]);

                if (method == "Withdraw") return Withdraw();

                if (method == "GetReceiver") return GetReceiver();

                if (method == "GetSender") return GetSender();

                if (method == "GetTime") return GetCurrentTime();
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
            if (TotalSupply() != 0) return false;
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("totalSupply", TotalSupplyValue);
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            asset.Put(Owner, TotalSupplyValue);
            //Transferred(null, Owner, TotalSupplyValue);  //not transfer the coin to the owner since it will be sell
            return true;
        }

        [DisplayName("getTime")]
        public static BigInteger GetCurrentTime()
        {
            return (BigInteger)Runtime.Time;
        }

        //IS THE AMOUNT STILL USEFUL ?
        private static bool Init(int amount, BigInteger reservePrice, int durationBidding, int durationReveal)
        {
            if (!Runtime.CheckWitness(Owner)) return false;

            //Transferred(Owner, null, amount);
            Transferred(Owner, null, amount);

            //Auction
            Auction auction = new Auction(Runtime.Time, durationBidding, durationReveal, reservePrice, Owner);

            Storage.Put(Storage.CurrentContext, "auction", Serialize(auction));
            //StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));
            //revealed.Put(Owner, BoolToBytes(true));
            return true;

        }

        private static bool BidNonce(byte[] account, string hash, int nonce)
        {
            StorageMap balanceOf = Storage.CurrentContext.CreateMap(nameof(balanceOf));
            StorageMap hashedBidOf = Storage.CurrentContext.CreateMap(nameof(hashedBidOf));
            StorageMap nonceOf = Storage.CurrentContext.CreateMap(nameof(nonceOf));
            nonceOf.Put(account, nonce);

            if (Runtime.CheckWitness(Owner)) return false;

            byte[] endOfBidding = Storage.Get(Storage.CurrentContext, "endOfBidding");

            if (Runtime.Time < (uint)BytesToBigInteger(endOfBidding))
            {
                hashedBidOf.Put(account, hash);
            }

            return true;
        }

        private static bool Bid(byte[] bidderAddress, string hash)
        {
            if (Runtime.CheckWitness(Owner)) return false;

            //Deserialize auction
            Auction auction = (Auction)Deserialize(Storage.Get(Storage.CurrentContext, "auction"));
            if (Runtime.Time >= auction.endOfBidding) return false;

            //Store bidder's info
            Bidder bidder = new Bidder(bidderAddress, hash);
            auction.AddBidder(bidder);

            //Store bidder's info
            //ulong value = 0;
            //Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            //TransactionOutput reference = tx.GetReferences()[0];

            ////if (reference.AssetId != neo_asset_id) return 0;          //accept NEO
            ////if (reference.AssetId != gas_asset_id) return 0;        //accept GAS
            //byte[] sender = reference.ScriptHash;
            //byte[] receiver = ExecutionEngine.ExecutingScriptHash;

            //TransactionOutput[] outputs = tx.GetOutputs();
            //// get the total amount of Neo
            //foreach (TransactionOutput output in outputs)
            //{
            //    if (output.ScriptHash == receiver)
            //    {
            //        value += (ulong)output.Value;
            //    }
            //}

            return true;
        }

        private static bool Claim()
        {
            //TODO
            byte[] endOfRevealing = Storage.Get(Storage.CurrentContext, "endOfRevealing");
            byte[] highBidder = Storage.Get(Storage.CurrentContext, "highBidder");
            byte[] amount = Storage.Get(Storage.CurrentContext, "amount");

            if (!Runtime.CheckWitness(highBidder)) return false;

            if (Runtime.Time >= (uint)BytesToBigInteger(endOfRevealing))
            {
                Transferred(null, highBidder, (int)BytesToBigInteger(amount));
                return true;

            }

            return false;

        }

        //not necessary in my opinion
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (to.Length != 20) return false;

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from == to) return true;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        private static bool Reveal(byte[] senderAddress, int stake, int nonce)
        {
            //Get auction
            Auction auction = (Auction)Deserialize(Storage.Get(Storage.CurrentContext, "auction"));
            uint now = Runtime.Time;
            if (now < auction.endOfBidding || now >= auction.endOfRevealing) return false;

            //Get bidder's hash
            string hash = auction.GetBidderHash(senderAddress);
            if (hash == null) return false;

            //Compute hash and compare
            byte[] byteHash = AppendByteArrays(BitConverter.GetBytes(stake), BitConverter.GetBytes(nonce));
            string generatedHash = GenerateSHA256String(byteHash);
            if (hash != generatedHash) return false;

           

            //TODO : ??? BALANCE OF SENDER > AMOUNT (NOT SURE IF NEEDED) ???
            StorageMap balanceOf = Storage.CurrentContext.CreateMap(nameof(balanceOf));
            StorageMap hashedBidOf = Storage.CurrentContext.CreateMap(nameof(hashedBidOf));

            //store the amount of neo sent
            ulong value = 0;
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput reference = tx.GetReferences()[0];
            // if (reference.AssetId != neo_asset_id) return 0;          //accept NEO
            //if (reference.AssetId != gas_asset_id) return 0;        //accept GAS
            byte[] sender = reference.ScriptHash;
            byte[] receiver = ExecutionEngine.ExecutingScriptHash;

            TransactionOutput[] outputs = tx.GetOutputs();
            // get the total amount of Neo
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == receiver)
                {
                    value += (ulong)output.Value;
                }
            }
            balanceOf.Put(sender, value);

            //Add sender to revealed
            StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));
            revealed.Put(sender, BoolToBytes(true));

            //check highest bid
            BigInteger highBid = Storage.Get(Storage.CurrentContext, "highBid").AsBigInteger();
            BigInteger secondBid = Storage.Get(Storage.CurrentContext, "secondBid").AsBigInteger();
            //check if highest bid
            if (value > (ulong)highBid)
            {
                //refund previous higher

                //update highest and second highest
                Storage.Put(Storage.CurrentContext, "secondBid", highBid);
                Storage.Put(Storage.CurrentContext, "highBid", value);
                Storage.Put(Storage.CurrentContext, "highBidder", sender);

                //transfer the money
            }
            else if (value > (ulong)secondBid)
            {
                //in programtheblockchain they refund something not really sure why

                //update second bid
                Storage.Put(Storage.CurrentContext, "secondBid", value);
            }

            return true;
        }

        private static bool Withdraw()
        {
            //check timing period
            byte[] endOfRevealing = Storage.Get(Storage.CurrentContext, "endOfRevealing");
            if (Runtime.Time < (uint)BytesToBigInteger(endOfRevealing)) return false;

            //check caller has revealed his bid
            byte[] caller = GetCaller();
            StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));

            //TODO ERROR HERE WITH BITCONVERTER --> NEED TO GET REVEALED VALUE TRUE OR FALSE
            // if (!BitConverter.ToBoolean((byte[]) revealed.Get(caller), 0)) return false;

            //transfer money from owner to caller
            StorageMap balanceOf = Storage.CurrentContext.CreateMap(nameof(balanceOf));
            Transfer(Owner, caller, balanceOf.Get(caller).AsBigInteger());
            return true;
        }

        private static bool IsPayable(byte[] to)
        {
            var c = Blockchain.GetContract(to);
            return c == null || c.IsPayable;
        }

        private static string BytesToString(byte[] data) => data.AsString();

        private static byte[] BoolToBytes(bool val) => val ? (new byte[1] { 1 }) : (new byte[1] { 0 });

        private static BigInteger BytesToBigInteger(byte[] data) => data.AsBigInteger();

        // private static byte[] GenerateHash(byte[] amount, byte[] nonce)
        // {
        //     HashAlgorithm algorithm = new SHA256Managed();

        //     byte[] plainTextWithSaltBytes =
        //         new byte[amount.Length + nonce.Length];

        //     for (int i = 0; i < amount.Length; i++)
        //     {
        //         plainTextWithSaltBytes[i] = amount[i];
        //     }
        //     for (int i = 0; i < nonce.Length; i++)
        //     {
        //         plainTextWithSaltBytes[amount.Length + i] = nonce[i];
        //     }

        //     return algorithm.ComputeHash(plainTextWithSaltBytes);
        // }

        
        private static string GenerateSHA256String(byte[] input)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] hash = sha256.ComputeHash(input);
            return GetStringFromHash(hash);
        }

        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder str = new StringBuilder();
            foreach(byte b in hash)
            {
                str.Append(b.ToString("X2"));
            }
            return str.ToString();
        }
        private static byte[] AppendByteArrays(byte[] array1, byte[] array2)
        {
            byte[] array3 = new byte[array1.Length + array2.Length];

            //append first array
            for(int i = 0; i<array1.Length; i++)
            {
                array3[i] = array1[i];
            }

            //append 2nd array
            for(int i = array1.Length; i<array3.Length; i++)
            {
                array3[i] = array2[i - array1.Length];
            }

            return array3;
        }
        private static bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length) return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }

        [DisplayName("getReceiver")]
        public static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        [DisplayName("getCaller")]
        public static byte[] GetCaller()
        {
            return ExecutionEngine.CallingScriptHash;
        }

        [DisplayName("getSender")]
        public static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            // you can choice refund or not refund
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == neo_asset_id) return output.ScriptHash;
            }
            return new byte[1] { 0x20 };
        }

        private static byte[] Serialize(object obj)
        {
            if (obj == null) return null;

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private static Object Deserialize(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                return bf.Deserialize(ms);
            }
        }
    }
}
