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
    public int stake { get; set; }
    public string hash { get; set; }
    public bool hasRevealed { get; set; }
    public bool hasAnnounced { get; set; }

    public Bidder(byte[] address, string hash)
    {
        this.address = address;
        this.hash = hash;
    }

    public Bidder(byte[] address)
    {
        this.address = address;
        this.hasAnnounced = true;
    }
}
