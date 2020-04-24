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
using System.Collections;

[Serializable()]
public class Bidder
{
    public byte[] address { get; set; }
    public ulong value { get; set; }
    public string hash { get; set; }
    public bool hasRevealed { get; set; }

    public Bidder(byte[] address, ulong value, string hash)
    {
        this.address = address;
        this.value = value;
        this.hash = hash;
        this.hasRevealed = false;
    }
    public Bidder(byte[] address, string hash)
    {
        this.address = address;
        this.hash = hash;
    }
}
