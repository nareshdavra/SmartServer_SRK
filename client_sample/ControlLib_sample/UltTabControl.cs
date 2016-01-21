using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ControlLib
{
    public partial class UltraTabControl : Infragistics.Win.UltraWinTabControl.UltraTabControl
    {
        private bool _mResetProperty;
        /// <summary>
        /// Reset Default Property
        /// </summary>
        [Category("UserDefine"), Browsable(true)]
        public bool ResetProperty
        {
            get { return _mResetProperty; }
            set { _mResetProperty = value; }
        }

        public UltraTabControl()
        {
            _mResetProperty = true;
            SetDefault();
            InitializeComponent();
        }

        protected override void InitLayout()
        {
            _mResetProperty = true;
            SetDefault();
            base.InitLayout();
        }

        private void SetDefault()
        {
            if (_mResetProperty)
            {
                this.ActiveTabAppearance.BackColor = System.Drawing.Color.LightSteelBlue;
                this.ActiveTabAppearance.BorderColor = System.Drawing.Color.Black;
                this.ActiveTabAppearance.BorderColor3DBase = System.Drawing.Color.White;
                this.ActiveTabAppearance.FontData.BoldAsString = "True";
                this.ActiveTabAppearance.ForeColor = System.Drawing.Color.Black;

                this.Appearance.BorderColor3DBase = System.Drawing.Color.White;
                this.Appearance.FontData.BoldAsString = "False";
                this.Appearance.FontData.Name = "Arial";
                this.Appearance.FontData.SizeInPoints = 8F;
                this.Appearance.ForeColor = System.Drawing.Color.Black;

                this.AutoSelect = true;
                this.AutoSelectDelay = 5000;

                this.ClientAreaAppearance.BackColor = System.Drawing.SystemColors.Control;
                this.ClientAreaAppearance.BackColor2 = System.Drawing.SystemColors.Control;
                this.ClientAreaAppearance.BorderColor = System.Drawing.Color.Black;

                this.CloseButtonAppearance.BackColor = System.Drawing.Color.Transparent;
                this.CloseButtonAppearance.ForeColor = System.Drawing.Color.Black;

                this.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                this.Style = Infragistics.Win.UltraWinTabControl.UltraTabControlStyle.PropertyPage2003;
                this.TabButtonStyle = Infragistics.Win.UIElementButtonStyle.Windows8Button;
                this.TabCloseButtonVisibility = Infragistics.Win.UltraWinTabs.TabCloseButtonVisibility.WhenSelectedOrHotTracked;
                this.TabOrientation = Infragistics.Win.UltraWinTabs.TabOrientation.TopLeft;
                this.TabPageMargins.Bottom = 1;
                this.TabPageMargins.Left = 1;
                this.TabPageMargins.Right = 1;
                this.TabPageMargins.Top = 1;
                this.UseFlatMode = Infragistics.Win.DefaultableBoolean.False;
                this.UseOsThemes = Infragistics.Win.DefaultableBoolean.True;
                this.ViewStyle = Infragistics.Win.UltraWinTabControl.ViewStyle.Default;
            }
        }
    }
}
