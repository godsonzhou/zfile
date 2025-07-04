// ListSheet.cpp : implementation file
//

#include "stdafx.h"
#include "ListSheet.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CListSheet

IMPLEMENT_DYNAMIC(CListSheet, CPropertySheetRz)

CListSheet::CListSheet(LPCTSTR pszCaption, CWnd* pParentWnd, UINT iSelectPage)
   :CPropertySheetRz(pszCaption, pParentWnd, iSelectPage)
{
	AddPage( &m_fi );
	AddPage( &m_fi2 );
//	m_fi2.m_psp.dwFlags |= PSP_USETITLE;
//	m_fi2.m_psp.pszTitle ="Image File Header";
}

BOOL CListSheet::SetPageTitle (int nPage, LPTSTR pszText)
{
    CTabCtrl* pTab = GetTabControl();
    ASSERT (pTab);

    TC_ITEM ti;
    ti.mask = TCIF_TEXT;
    ti.pszText = pszText;
    VERIFY (pTab->SetItem (nPage, &ti));

    return TRUE;
}

CListSheet::~CListSheet()
{
	TRACE0("CListSheet : destructor \n");
	CleanUp();
}

void CListSheet::OnDestroy() 
{
	TRACE0("CListSheet : OnDestroy \n");
}

void CListSheet::Renew(PE_EXE *pPE) 
{
	TRACE0("CListSheet : Renew \n");
	CleanUp();
	m_fi.Renew(pPE);
	m_fi2.Renew(pPE);
}

void CListSheet::CleanUp()
{
	TRACE0("CListSheet : CleanUp \n");
	UINT i=GetPageCount();
	SetActivePage( 0 );
	if (i--)
		for(i; i>1; i--)
		{
//			((CResizePage *)GetPage( i ))->Init();
			RemovePage(i); 
		}
}

#define WM_RESIZEPAGE (WM_USER + 111)
BEGIN_MESSAGE_MAP(CListSheet, CPropertySheetRz)
   //{{AFX_MSG_MAP(CListSheet)
   	ON_WM_DESTROY()
   //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CListSheet message handlers