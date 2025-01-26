using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Sheng.Winform.Controls
{
    
    public class ShengPanel : Panel, IShengValidate
    {
        #region ��������

        private bool showBorder = false;
        /// <summary>
        /// �Ƿ���ʾ�߿�
        /// </summary>
        public bool ShowBorder
        {
            get
            {
                return this.showBorder;
            }
            set
            {
                this.showBorder = value;

                InitBrush();
                InitPen();

                this.Invalidate();
            }
        }

        private Color borderColor = Color.Black;
        /// <summary>
        /// �߿���ɫ
        /// </summary>
        public Color BorderColor
        {
            get
            {
                return this.borderColor;
            }
            set
            {
                this.borderColor = value;

                InitPen();
            }
        }

        private Color fillColorStart;
        /// <summary>
        /// ���ɫ(��ʼ)
        /// ����ǵ�ɫ���,ʹ�ô���ɫ
        /// </summary>
        public Color FillColorStart
        {
            get
            {
                if (this.fillColorStart == null)
                {
                    this.fillColorStart = this.Parent.BackColor;
                }
                return this.fillColorStart;
            }
            set
            {
                this.fillColorStart = value;

                InitBrush();
            }
        }

        private Color fillColorEnd;
        /// <summary>
        /// ���ɫ����
        /// </summary>
        public Color FillColorEnd
        {
            get
            {
                if (this.fillColorEnd == null)
                {
                    this.fillColorEnd = this.Parent.BackColor;
                }

                return this.fillColorEnd;
            }
            set
            {
                this.fillColorEnd = value;

                InitBrush();
            }
        }

        private Brush fillBrush;
        /// <summary>
        /// ������
        /// </summary>
        public Brush FillBrush
        {
            get
            {
                return this.fillBrush;
            }
            set
            {
                this.fillBrush = value;
                this.Invalidate();
            }
        }

        private Pen borderPen;
        /// <summary>
        /// �߿򻭱�
        /// </summary>
        public Pen BorderPen
        {
            get
            {
                return this.borderPen;
            }
            set
            {
                this.borderPen = value;
                this.Invalidate();
            }
        }

        private FillStyle fillStyle = FillStyle.Solid;
        /// <summary>
        /// �����ʽ
        /// ��ɫ�򽥱�
        /// </summary>
        public FillStyle FillStyle
        {
            get
            {
                return this.fillStyle;
            }
            set
            {
                this.fillStyle = value;

                InitBrush();
            }
        }

        private LinearGradientMode fillMode;
        /// <summary>
        /// ���ģʽ
        /// ����ǽ���,���ĸ�����Ľ�����ѡһ��
        /// </summary>
        public LinearGradientMode FillMode
        {
            get
            {
                return this.fillMode;
            }
            set
            {
                this.fillMode = value;

                InitBrush();
            }
        }

        #endregion

        #region ˽�г�Ա

        /// <summary>
        /// ���Rectangle
        /// </summary>
        private Rectangle FillRectangle
        {
            get
            {
                if (this.ClientRectangle == new Rectangle())
                {
                    return new Rectangle(0, 0, 1, 1);
                }

                Rectangle rect;

                if (this.ShowBorder)
                {
                    rect = new Rectangle(1, 1, this.ClientRectangle.Width - 1, this.ClientRectangle.Height - 1);
                }
                else
                {
                    rect = this.ClientRectangle;
                }

                if (rect.Width == 0)
                {
                    rect.Width++;
                }
                if (rect.Height == 0)
                {
                    rect.Height++;
                }

                return rect;
            }
        }

        /// <summary>
        /// ����Rectangle
        /// </summary>
        private Rectangle DrawRectangle
        {
            get
            {
                if (this.ClientRectangle == new Rectangle())
                {
                    return new Rectangle(0, 0, 1, 1);
                }

                return new Rectangle(0, 0, this.ClientRectangle.Width - 1, this.ClientRectangle.Height - 1);
            }
        }

        #endregion

        #region ����

        public ShengPanel()
        {

            EnableDoubleBuffering();

            this.FillBrush = new SolidBrush(this.FillColorStart);
            this.BorderPen = new Pen(this.BorderColor);;
        }

        #endregion

        #region ˽�з���

        /// <summary>
        /// ����˫������
        /// </summary>
        private void EnableDoubleBuffering()
        {
            // Set the value of the double-buffering style bits to true.
            this.SetStyle(ControlStyles.DoubleBuffer |
               ControlStyles.UserPaint |
               ControlStyles.AllPaintingInWmPaint,
               true);
            this.UpdateStyles();
        }

        /// <summary>
        /// ��ʼ��Pen
        /// </summary>
        private void InitPen()
        {
            if (this.ShowBorder)
            {
                this.BorderPen = new Pen(this.BorderColor);
            }
        }

        /// <summary>
        /// ��ʼ��Brush
        /// </summary>
        private void InitBrush()
        {
            if (this.FillStyle == FillStyle.Solid)
            {
                this.FillBrush = new SolidBrush(this.FillColorStart);
            }
            else
            {
                this.FillBrush = new LinearGradientBrush(this.FillRectangle, this.FillColorStart, this.FillColorEnd, this.FillMode);
            }
        }

        /// <summary>
        /// ���ƿؼ�
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);

            e.Graphics.FillRectangle(this.FillBrush, this.FillRectangle);

            if (this.ShowBorder)
            {
                e.Graphics.DrawRectangle(this.BorderPen, this.DrawRectangle);
            }
        }

        /// <summary>
        /// ��С�ı�
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {

            base.OnResize(e);

            if (this.Height <= 0 || this.Width <= 0)
            {
                return;
            }

            InitBrush();
            InitPen();

            this.Invalidate();
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

        private bool highLight = false;
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
        /// ��֤�ؼ�
        /// </summary>
        /// <param name="validateMsg"></param>
        /// <returns></returns>
        public bool SEValidate(out string validateMsg)
        {
            return ShengValidateHelper.ValidateContainerControl(this, out validateMsg);
        }

        public CustomValidateMethod CustomValidate
        {
            get;
            set;
        }

        #endregion
    }
}
