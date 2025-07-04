// source http://web.mit.edu/tytso/www/pilot/prc-format.html
// http://www.palmos.com/dev/support/docs/fileformats/Intro.html
#include "stdafx.h"
#include "..\libdump\palm.h"

CString PilottoDate(ULONG nbsec)
{
	COleDateTime RefMacTime(1904,1,1,0,0,0);
	COleDateTime MacTime;

	MacTime = RefMacTime + COleDateTimeSpan((double) nbsec/(24L*3600));
	return MacTime.Format("%A %d %B %Y - %H %M %S");
}

CString UInttoStr(long val)
{
	CString str;
	str.Format("%c%c%c%c", val & 0xFF, (val >> 8) & 0xFF, (val >> 16) & 0xFF, (val >> 24) & 0xFF);
	return str;
}

inline short inv(short val)
{
	return ((val & 0xFF) << 8) 
		+ ((val >> 8) & 0xFF);
}

inline long inv(long val)
{
	return ((val & 0xFF) << 24) 
		+ (((val >> 8) & 0xFF) << 16)
		+ (((val >> 16) & 0xFF) << 8)
		+ (((val >> 24) & 0xFF));/**/
}


#define rtfReturn "\r\n"
CString DumpPalmHeader( DatabaseHdrType *pdBHt)
{
	CString str="", strTemp="";    
//	int j;

	str += (CString) ("Header Information :")+ rtfReturn+ rtfReturn;

	strTemp.Format("\tName of database:\t%s", pdBHt->name);
	str += strTemp + rtfReturn;
    strTemp.Format("\tattribute flags :\t%04Xh ", pdBHt->attributes);
	str += strTemp + rtfReturn;

	strTemp.Format("\tversion :\t%04Xh \t(%d.%d)", pdBHt->version & 0xFFFF, (pdBHt->version) & 0xFF, (pdBHt->version >> 8) & 0xFF);
	str += strTemp + rtfReturn;

	strTemp.Format("\tcreation Date :\t%08Xh \t", (pdBHt->creationDate));
	str += strTemp + (pdBHt->creationDate?PilottoDate(inv(pdBHt->creationDate)):"") + rtfReturn;	

	strTemp.Format("\tmodification Date :\t%08Xh\t", inv(pdBHt->modificationDate));
	str += strTemp + (pdBHt->modificationDate?PilottoDate(inv(pdBHt->modificationDate)):"") + rtfReturn;

    strTemp.Format("\tlastBackupDate :\t%08Xh \t", inv(pdBHt->lastBackupDate));
	str += strTemp + (pdBHt->lastBackupDate?PilottoDate(inv(pdBHt->lastBackupDate)):"") + rtfReturn;

    strTemp.Format("\tmodificationNumber :\t%08Xh ", pdBHt->modificationNumber);
	str += strTemp + rtfReturn;
    strTemp.Format("\tappInfoID :\t%08Xh ", pdBHt->appInfoID);
	str += strTemp + rtfReturn;
    strTemp.Format("\tsortInfoID :\t%08Xh ", pdBHt->sortInfoID);
	str += strTemp + rtfReturn;
    strTemp.Format("\tType :\t%08Xh \t\"%s\" ", pdBHt->type,UInttoStr(pdBHt->type));
	str += strTemp + rtfReturn;
    strTemp.Format("\tcreator :\t%08Xh \t\"%s\" ", pdBHt->creator,UInttoStr(pdBHt->creator));
	str += strTemp + rtfReturn;
    strTemp.Format("\tuniqueIDSeed :\t%08Xh ", pdBHt->uniqueIDSeed);
    str += strTemp + rtfReturn;
    strTemp.Format("\trecordList :\t%08Xh ", pdBHt->recordList);
    str += strTemp + rtfReturn;

/*
	str += rtfReturn;
    str += (CString) ("Extra Header Information :")+ rtfReturn;
   for (j=0; j<4; j++)
   {
      strTemp.Format("\tReserved WORD %d:\t%04Xh ", j, pdosHeader->e_res[j]);
      str += strTemp + rtfReturn;
   }
    strTemp.Format("\tOEM identifier :\t%04Xh ", pdosHeader->e_oemid);
    str += strTemp + rtfReturn;
    strTemp.Format("\tOEM information :\t%04Xh ", pdosHeader->e_oeminfo);
    str += strTemp + rtfReturn;
   for (j=0; j<10; j++)
   {
      strTemp.Format("\tReserved WORD %d:\t%04Xh ", j, pdosHeader->e_res2[j]);
      str += strTemp + rtfReturn;
   }
    strTemp.Format("\tNew Header Address :\t%08Xh ", pdosHeader->e_lfanew);
   str += strTemp + rtfReturn;

   ULONG size = pdosHeader->e_cp;
   size *= 512; size += pdosHeader->e_cblp;
    strTemp.Format("\t%s\t%lu B ( %lu KB )", "Memory Needed :", size, size >> 10);
   str += strTemp + rtfReturn;
*/
   return str;
}
