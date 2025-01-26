using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using Sheng.Winform.Controls.Win32;
using System.Drawing;

namespace Sheng.Winform.Controls
{
    
    public class ShengComboBox : ComboBox, IShengValidate
    {
        #region ��������

        private bool allowEmpty = true;
        /// <summary>
        /// �Ƿ������
        /// </summary>
        [Description("�Ƿ������")]
        [Category("Sheng.Winform.Controls")]
        public bool AllowEmpty
        {
            get
            {
                return this.allowEmpty;
            }
            set
            {
                this.allowEmpty = value;
            }
        }

        private string waterText = String.Empty;
        /// <summary>
        /// ˮӡ�ı�
        /// </summary>
        [Description("ˮӡ�ı�")]
        [Category("Sheng.Winform.Controls")]
        public string WaterText
        {
            get { return this.waterText; }
            set
            {
                this.waterText = value;
                this.Invalidate();
            }
        }

        #endregion

        #region ����

        public ShengComboBox()
        {
        }

        #endregion

        #region ˽�з���


        protected override void WndProc(ref   Message m)
        {
            base.WndProc(ref   m);

            if (m.Msg == User32.WM_PAINT || m.Msg == User32.WM_ERASEBKGND || m.Msg == User32.WM_NCPAINT)
            {
                if (!this.Focused && this.Text == String.Empty  && this.WaterText != String.Empty)
                {
                    Graphics g = Graphics.FromHwnd(this.Handle);
                    g.DrawString(this.WaterText, this.Font, Brushes.Gray, 2, 2);
                }
            }
        }

        #endregion

        #region ISEValidate ��Ա

        private string title;
        /// <summary>
        /// ����
        /// </summary>
        [Description("����")]
        [Category("Sheng.Winform.Controls")]
        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                this.title = value;
            }
        }

        private bool highLight = true;
        /// <summary>
        /// ��֤ʧ��ʱ�Ƿ���Ҫ������ʾ���ı䱳��ɫ��
        /// </summary>
        [Description("Ҫƥ���������ʽ")]
        [Category("Sheng.Winform.Controls")]
        public bool HighLight
        {
            get
            {
                return this.highLight;
            }
            set
            {
                this.highLight = value;
            }
        }

        /// <summary>
        /// ��֤�ؼ�������
        /// </summary>
        /// <returns></returns>
        public bool SEValidate(out string msg)
        {
            msg = String.Empty;

            if (!this.AllowEmpty && this.Text == "")
            {
                msg += String.Format("[ {0} ] {1}", this.Title, "������Ϊ��");
                return false;
            }

            #region CustomValidate

            if (CustomValidate != null)
            {
                string customValidateMsg;
                if (CustomValidate(this, out customValidateMsg) == false)
                {
                    msg += String.Format("[ {0} ] {1}", this.Title, customValidateMsg);
                    return false;
                }
            }

            #endregion

            return true;
        }

        public CustomValidateMethod CustomValidate
        {
            get;
            set;
        }

        #endregion
    }
}
