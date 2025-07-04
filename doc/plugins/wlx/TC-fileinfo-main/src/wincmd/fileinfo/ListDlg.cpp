// ListDlg.cpp : implementation file
//

#include "stdafx.h"
#include "ListDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// ListDlg dialog


ListDlg::ListDlg(CStringList *pstrl,CWnd* pParent /*=NULL*/)
	: CDialog(ListDlg::IDD, pParent)
{
	//{{AFX_DATA_INIT(ListDlg)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
	m_pstrl = pstrl;
}


void ListDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(ListDlg)
	DDX_Control(pDX, IDC_LIST1, m_list);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(ListDlg, CDialog)
	//{{AFX_MSG_MAP(ListDlg)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// ListDlg message handlers

BOOL ListDlg::OnInitDialog() 
{
	CDialog::OnInitDialog();
    POSITION pos;
    for( pos = m_pstrl->GetHeadPosition(); pos != NULL; )
         m_list.AddString( m_pstrl->GetNext( pos ));
	return TRUE;  // return TRUE unless you set the focus to a control
	              // EXCEPTION: OCX Property Pages should return FALSE
}
