/*
    else if (memcmp(header,"PMW1",4) == 0)
        throwCantPack("already packed with PMWLITE");
03E0

*/
#define UPX_MAGIC_LE32      0x21585055          /* "UPX!" */
#define UPX_MAGIC2_LE32     0xD5D0D8A1

if (pfind_le32(ibuf,UPX_MIN(imagesize,128u),UPX_MAGIC_LE32))

upx_bytep pfind_le32(const void *b, int blen, unsigned what)
{
    return pfind(b, blen, what, 4);
}

BOOL pfind(const void *b, int blen, UPX_MAGIC_LE32, int wlen)
{
    if (b == NULL || blen <= 0 || what == NULL || wlen <= 0)
        return FALSE;

    int i;
    const upx_bytep base = (const upx_bytep) b;
    unsigned char firstc = * (const upx_bytep) what;

    blen -= wlen;
    for (i = 0; i <= blen; i++, base++)
        if (*base == firstc && memcmp(base, what, wlen) == 0)
            return const_cast<upx_bytep>(base);

    return NULL;
}
