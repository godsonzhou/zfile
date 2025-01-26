using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Drawing;
using Sheng.Winform.Controls.Win32;

namespace Sheng.Winform.Controls
{
    
    public class ShengTextBox : TextBox, IShengValidate
    {
        #region ����

        public ShengTextBox()
        {
       }

        #endregion

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

        private string regex = String.Empty;
        /// <summary>
        /// Ҫƥ���������ʽ
        /// </summary>
        [Description("Ҫƥ���������ʽ")]
        [Category("Sheng.Winform.Controls")]
        public string Regex
        {
            get
            {
                if (this.regex == null)
                {
                    this.regex = String.Empty;
                }
                return this.regex;
            }
            set
            {
                this.regex = value;
            }
        }

        private string regexMsg;
        /// <summary>
        /// ������֤��ͨ��ʱ����ʾ��Ϣ
        /// </summary>
        [Description("������֤��ͨ��ʱ����ʾ��Ϣ")]
        [Category("Sheng.Winform.Controls")]
        public string RegexMsg
        {
            get
            {
                return this.regexMsg;
            }
            set
            {
                this.regexMsg = value;
            }
        }



        private ShengTextBox valueCompareTo;
        /// <summary>
        /// ��ָ����SETextBox��ֵ���Ƚϣ�������ͬ
        /// �����������������
        /// </summary>
        [Description("��ָ����SETextBox��ֵ���Ƚϣ�������ͬ")]
        [Category("Sheng.Winform.Controls")]
        public ShengTextBox ValueCompareTo
        {
            get
            {
                return this.valueCompareTo;
            }
            set
            {
                this.valueCompareTo = value;
            }
        }

        private bool limitMaxValue = false;
        /// <summary>
        /// ��ֻ�����������ֵ������,�Ƿ��������ֵ
        /// </summary>
        [Description("��ֻ�����������ֵ������,�Ƿ��������ֵ")]
        [Category("Sheng.Winform.Controls")]
        public bool LimitMaxValue
        {
            get { return this.limitMaxValue; }
            set { this.limitMaxValue = value; }
        }

        private long maxValue = Int32.MaxValue;
        /// <summary>
        /// ��ֻ�����������ֵ������,��������ֵ
        /// </summary>
        [Description("��ֻ�����������ֵ������,��������ֵ")]
        [Category("Sheng.Winform.Controls")]
        public long MaxValue
        {
            get { return this.maxValue; }
            set { this.maxValue = value; }
        }

        #endregion

        #region ˽�з���

        protected override void WndProc(ref   Message m)
        {
            base.WndProc(ref   m);

            if (m.Msg == User32.WM_PAINT || m.Msg == User32.WM_ERASEBKGND || m.Msg == User32.WM_NCPAINT)
            {
                if (!this.Focused && this.Text == String.Empty && this.WaterText != String.Empty)
                {
                    Graphics g = Graphics.FromHwnd(this.Handle);
                    g.DrawString(this.WaterText, this.Font, Brushes.Gray, this.ClientRectangle);
                }
            }
        }

        #endregion

        #region ��������


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
        [Description("��֤ʧ��ʱ�Ƿ���Ҫ������ʾ���ı䱳��ɫ��")]
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

            #region �Ƿ�Ϊ��

            if (!this.AllowEmpty && this.Text == "")
            {
                msg += String.Format("[ {0} ] {1}", this.Title, "������Ϊ��");
                return false;
            }

            #endregion

            #region ����

            if (this.Text != "" && this.Regex != String.Empty)
            {
                System.Text.RegularExpressions.Regex r = new Regex(this.Regex, RegexOptions.Singleline);
                Match m = r.Match(this.Text);
                if (m.Success == false)
                {
                    msg += String.Format("[ {0} ] {1}", this.Title, this.RegexMsg);
                    return false;
                }
            }

            #endregion

            #region ��ֵ��Χ

            if (LimitMaxValue && this.Text != String.Empty)
            {
                Regex regex = new Regex(@"^\d+$");
                Match match = regex.Match(this.Text);
                if (match.Success)
                {
                    long value = Int64.Parse(this.Text);
                    if (value > this.MaxValue)
                    {
                        msg += String.Format("[ {0} ] {1}", this.Title, "���ܴ��� " + this.MaxValue.ToString());
                        return false;
                    }
                }
            }

            #endregion

            #region CompareTo

            if (this.ValueCompareTo != null)
            {
                if (this.Text != this.ValueCompareTo.Text)
                {
                    msg += String.Format("[ {0} ] �� [ {1} ] {2}", this.Title, this.ValueCompareTo.Title, "���������ݱ�����ͬ");
                    this.ValueCompareTo.BackColor = Color.Pink;
                    return false;
                }
            }

            #endregion

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

            msg = String.Empty;
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
