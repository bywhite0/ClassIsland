using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 输入数值的文本框
/// </summary>
public class NumbericTextBox : TextBox
{
    #region Fields

    #region DependencyProperty

    /// <summary>
    /// 最大值的依赖属性
    /// </summary>
    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        "MaxValue",
        typeof(double),
        typeof(NumbericTextBox),
        new PropertyMetadata(double.MaxValue));

    /// <summary>
    /// 最小值的依赖属性
    /// </summary>
    public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
        "MinValue",
        typeof(double),
        typeof(NumbericTextBox),
        new PropertyMetadata(double.MinValue));

    /// <summary>
    /// 精度的依赖属性
    /// </summary>
    public static readonly DependencyProperty PrecisionProperty = DependencyProperty.Register(
        "Precision",
        typeof(ushort),
        typeof(NumbericTextBox),
        new PropertyMetadata((ushort)2));

    #endregion DependencyProperty

    /// <summary>
    /// 先前合法的文本
    /// </summary>
    private string lastLegalText;

    /// <summary>
    /// 是否为粘贴
    /// </summary>
    private bool isPaste;

    public event EventHandler<TextChangedEventArgs> PreviewTextChanged;

    #endregion Fields

    #region Constructor

    /// <summary>
    /// 构造函数
    /// </summary>
    public NumbericTextBox()
    {
        PreviewTextInput += NumbericTextBoxPreviewTextInput;
        TextChanged += NumbericTextBoxTextChanged;
        PreviewKeyDown += NumbericTextBox_PreviewKeyDown;
        LostFocus += NumbericTextBoxLostFocus;
        InputMethod.SetIsInputMethodEnabled(this, false);

        Loaded += NumbericTextBoxLoaded;
    }

    #endregion Constructor

    #region Properties

    /// <summary>
    /// 最大值,可取
    /// </summary>
    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    /// <summary>
    /// 最小值,可取
    /// </summary>
    public double MinValue
    {
        get => (double)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    /// <summary>
    /// 精度,即精确到小数点后的位数
    /// </summary>
    public ushort Precision
    {
        get => (ushort)GetValue(PrecisionProperty);
        set => SetValue(PrecisionProperty, value);
    }

    #endregion Properties

    protected virtual void OnPreviewTextChanged(TextChangedEventArgs e)
    {
        if (PreviewTextChanged != null) PreviewTextChanged(this, e);
    }

    #region Private Methods

    /// <summary>
    /// 处理粘贴的情况
    /// </summary>
    protected virtual void HandlePaste()
    {
        isPaste = false;

        // 处理符号的标志
        var handledSybmol = false;

        // 处理小数点的标志
        var handledDot = false;

        // 当前位对应的基数
        double baseNumber = 1;

        // 转换后的数字
        double number = 0;

        // 上一次合法的数字
        double lastNumber = 0;

        // 小数点后的位数
        double precision = 0;
        foreach (var c in Text)
        {
            if (!handledSybmol && c == '-')
            {
                baseNumber = -1;
                handledSybmol = true;
            }

            if (c >= '0' && c <= '9')
            {
                var digit = c - '0';
                if (!handledDot)
                {
                    number = number * baseNumber + digit;
                    baseNumber = 10;
                }
                else
                {
                    baseNumber = baseNumber / 10;
                    number += digit * baseNumber;
                }

                // 正负号必须位于最前面
                handledSybmol = true;
            }

            if (c == '.')
            {
                // 精度已经够了
                if (precision + 1 > Precision) break;

                handledDot = true;

                // 此时正负号不能起作用
                handledSybmol = true;
                baseNumber = 0.1;
                precision++;
            }

            if (number < MinValue || number > MaxValue)
            {
                Text = lastNumber.ToString(CultureInfo.InvariantCulture);
                SelectionStart = Text.Length;
                return;
            }

            lastNumber = number;
        }

        Text = number.ToString(CultureInfo.InvariantCulture);
        SelectionStart = Text.Length;
    }

    #endregion Private Methods

    #region Overrides of TextBoxBase

    #endregion

    #region Events Handling

    private void NumbericTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (MinValue > MaxValue) MinValue = MaxValue;

        if (string.IsNullOrEmpty(Text))
        {
            var val = (MaxValue + MinValue) / 2;
            val = Math.Round(val, Precision);

            Text = val.ToString(CultureInfo.InvariantCulture);
        }

        isPaste = true;
    }

    /// <summary>
    /// The numberic text box preview text input.
    /// </summary>
    /// <param name="sender"> The sender.</param>
    /// <param name="e"> The e.</param>
    private void NumbericTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 如果是粘贴不会引发该事件
        isPaste = false;

        short val;

        // 输入非数字
        if (!short.TryParse(e.Text, out val))
        {
            // 小于0时,可输入负号
            if (MinValue < 0 && e.Text == "-")
            {
                var minusPos = Text.IndexOf('-');

                // 未输入负号且负号在第一位
                if (minusPos == -1 && SelectionStart == 0) return;
            }

            // 精度大于0时,可输入小数点
            if (Precision > 0 && e.Text == ".")
            {
                // 解决UpdateSourceTrigger为PropertyChanged时输入小数点文本与界面不一致的问题
                if (SelectionStart > Text.Length)
                {
                    e.Handled = true;
                    return;
                }

                // 小数点位置
                var dotPos = Text.IndexOf('.');

                // 未存在小数点可输入
                if (dotPos == -1) return;

                // 已存在小数点但处于选中状态,也可输入小数点
                if (SelectionStart >= dotPos && SelectionLength > 0) return;
            }

            e.Handled = true;
        }
        else
        {
            var dotPos = Text.IndexOf('.');
            var cursorIndex = SelectionStart;

            // 已经存在小数点,且小数点在光标后
            if (dotPos != -1 && dotPos < cursorIndex)
                // 不允许输入超过精度的数
                if (Text.Length - dotPos > Precision && SelectionLength == 0)
                    e.Handled = true;
        }
    }

    /// <summary>
    /// The numberic text box text changed.
    /// </summary>
    /// <param name="sender"> The sender.</param>
    /// <param name="e"> The e.</param>
    private void NumbericTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (lastLegalText == Text) return;

        OnPreviewTextChanged(e);

        // 允许为空
        if (string.IsNullOrEmpty(Text)) return;

        // 粘贴而来的文本
        if (isPaste)
        {
            HandlePaste();
            lastLegalText = Text;

            return;
        }

        double val;
        if (double.TryParse(Text, out val))
        {
            // 保存光标位置
            var selectIndex = SelectionStart;
            if (val > MaxValue || val < MinValue)
            {
                Text = lastLegalText;
                SelectionStart = selectIndex;
                return;
            }

            lastLegalText = Text;
        }

        isPaste = true;
    }

    /// <summary>
    /// The numberic text box_ preview key down.
    /// </summary>
    /// <param name="sender"> The sender.</param>
    /// <param name="e"> The e.</param>
    private void NumbericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 过滤空格
        if (e.Key == Key.Space) e.Handled = true;
    }

    /// <summary>
    /// The numberic text box_ lost focus.
    /// </summary>
    /// <param name="sender"> The sender.</param>
    /// <param name="e"> The e.</param>
    private void NumbericTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Text)) Text = lastLegalText;
    }

    #endregion Events Handling
}