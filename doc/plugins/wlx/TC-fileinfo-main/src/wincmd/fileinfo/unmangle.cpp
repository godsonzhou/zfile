#include "stdafx.h"
#include "um.h"
#include <imagehlp.h>

#define DECAL 14
PSTR Undecorate(PSTR Textin, PSTR Textout, int len)
{
	int dec=0;
/*	if (!strncmp("__imp_", Textin, 6))
	{ 
		dec=6;
		strncpy(Textout, "IMP   ",DECAL);
	}
*/
	if ( *(Textin+dec) == '?')      // Microsoft C++
		UnDecorateSymbolName(Textin+dec, Textout+dec, len, 0);
    else if ( Textin[0] == '@')   //Borland C++
    {
		switch  (unmangle(Textin, Textout+DECAL, len-DECAL, 0, 0, 1))
        {
        case    UM_NOT_MANGLED:
            break;

        case    UM_MEMBER_FN:
            strncpy(Textout, "Function      ",DECAL);
            break;

        case    UM_CONSTRUCTOR:
            strncpy(Textout, "Constructor   ",DECAL);
            break;

        case    UM_DESTRUCTOR:
            strncpy(Textout, "Destructor    ",DECAL);
            break;

        case    UM_OPERATOR:
            strncpy(Textout, "Operator      ",DECAL);
            break;

        case    UM_CONVERSION:
            strncpy(Textout, "Conversion    ",DECAL);
            break;

        case    UM_STATIC_DM:
            strncpy(Textout, "Static data   ",DECAL);
            break;

        case    UM_THUNK:
            strncpy(Textout, "Thunk         ",DECAL);
            break;

        case    UM_TYPEDESCR:
            strncpy(Textout, "Type Desc     ",DECAL);
            break;
        
		case    UM_VTABLE:
            strncpy(Textout, "VTable        ",DECAL);
            break;

        case    UM_OTHER:
            strncpy(Textout, "???????????   ",DECAL);
            break;
        } 
	}else strncpy(Textout, Textin, len);
//      TRACE1("undecorate : %s\n", Textout );
	return Textout;
} 


#ifdef  TEST

int main(int argc, char *argv[])
{
    char    buff[512];
    int i;

    if  (argc == 1)
    {
        argc    = 2;
        argv[1] = "@foo@bar@$bctr$q";
    }

    for (i = 1; i < argc; i++)
    {
//        printf("%-40s ", argv[i]);
        printf("%s ", argv[i]);
        Undecorate(argv[i], buff, 512, FALSE);
        printf(" -> %s\n", buff);
    }
    return  0;
}

#endif 
