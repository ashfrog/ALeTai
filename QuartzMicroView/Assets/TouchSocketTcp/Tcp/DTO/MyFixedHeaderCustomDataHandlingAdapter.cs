using System;
using TouchSocket.Core;

/// <summary>
/// 模板解析“固定包头”数据适配器
/// </summary>
public class MyFixedHeaderCustomDataHandlingAdapter : CustomFixedHeaderDataHandlingAdapter<DTOInfo>
{
    public MyFixedHeaderCustomDataHandlingAdapter()
    {
        MaxPackageSize = 1024 * 1024;
    }

    public override int HeaderLength => 12;

    protected override DTOInfo GetInstance()
    {
        return new DTOInfo();
    }
}

public class DTOInfo : IFixedHeaderRequestInfo
{
    private int bodyLength;
    private int dataType;
    private int orderType;
    private byte[] body;

    public int BodyLength => bodyLength;
    public int DataType => dataType;
    public int OrderType => orderType;
    public byte[] Body => body;

    public bool OnParsingBody(ReadOnlySpan<byte> value)
    {
        if (value.Length != bodyLength)
        {
            return false;
        }

        body = value.ToArray();
        return true;
    }

    public bool OnParsingHeader(ReadOnlySpan<byte> header)
    {
        bodyLength = ReadInt32LittleEndian(header) - 12;
        dataType = ReadInt32LittleEndian(header.Slice(4));
        orderType = ReadInt32LittleEndian(header.Slice(8));
        return bodyLength >= 0;
    }

    private static int ReadInt32LittleEndian(ReadOnlySpan<byte> value)
    {
        return value[0]
            | (value[1] << 8)
            | (value[2] << 16)
            | (value[3] << 24);
    }
}
