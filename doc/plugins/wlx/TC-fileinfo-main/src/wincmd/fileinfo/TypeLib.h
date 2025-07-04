// TypeLib.h: interface for the CTypeLib class.
//
//////////////////////////////////////////////////////////////////////

#if !defined(AFX_TYPELIB_H__96929745_B73B_4458_97BF_72C04BF14223__INCLUDED_)
#define AFX_TYPELIB_H__96929745_B73B_4458_97BF_72C04BF14223__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

class CTypeLib  
{
public:
	CTypeLib();
	virtual ~CTypeLib();
	static HRESULT GetTypeLibAsString(
		const LPTYPELIB& lpTypeLib, CString& sText
		);
	static HRESULT IsTypeLib(
		const CString& sTypeLibFileName, LPTYPELIB& lpTypeLib
		);

};

#endif // !defined(AFX_TYPELIB_H__96929745_B73B_4458_97BF_72C04BF14223__INCLUDED_)