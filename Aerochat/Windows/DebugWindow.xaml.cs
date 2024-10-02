using Aerobool.Expressions;
using Aerobool.Interface;
using Aerochat.Controls;
using Aerochat.Hoarder;
using Aerochat.Settings;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace Aerochat.Windows
{
    class StupidMouseOverThing(IExpression expression, int zIndex)
    {
        public IExpression Expression = expression;
        public int ZIndex = zIndex;
    }
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        private Action methodToExecute;
        private Func<bool> canExecuteEvaluator;
        public RelayCommand(Action methodToExecute, Func<bool> canExecuteEvaluator)
        {
            this.methodToExecute = methodToExecute;
            this.canExecuteEvaluator = canExecuteEvaluator;
        }
        public RelayCommand(Action methodToExecute)
            : this(methodToExecute, null)
        {
        }
        public bool CanExecute(object parameter)
        {
            if (this.canExecuteEvaluator == null)
            {
                return true;
            }
            else
            {
                bool result = this.canExecuteEvaluator.Invoke();
                return result;
            }
        }
        public void Execute(object parameter)
        {
            this.methodToExecute.Invoke();
        }
    }
    public partial class DebugWindow : Window
    {
        private Typeface typeface = new Typeface("Segoe UI");
        private IExpression? expression;
        private readonly List<StupidMouseOverThing> mouseOverExpressions = [];
        private int mouseX;
        private int mouseY;
        private int _currentZ = 0;
        private bool mouseOverNull = false;
        private string? nullProperty = null;
        public DebugWindowViewModel ViewModel { get; } = new DebugWindowViewModel();
        public DebugWindow()
        {
            InitializeComponent();
            mouseX = (int)Mouse.GetPosition(this).X;
            mouseY = (int)Mouse.GetPosition(this).Y;
            expression = new AndExpression(new TypecheckExpression(new ValueExpression("channel"), new ValueExpression(typeof(DiscordDmChannel))), new NotExpression(new EqualsExpression(new ValueExpression(1), new ValueExpression(1))));
            Invalidate();
            MouseMove += DebugWindow_MouseMove;
            PreviewMouseRightButtonUp += DebugWindow_PreviewMouseRightButtonUp;
            PreviewMouseMove += DebugWindow_PreviewMouseMove;
        }

        private void DebugWindow_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || Cursor == Cursors.Arrow) return;
            // scroll _scrolledX and _scrolledY based on the difference between the current mouse position and the last mouse position
            var x = (int)e.GetPosition(this).X;
            var y = (int)e.GetPosition(this).Y;
            _scrolledX -= mouseX - x;
            _scrolledY -= mouseY - y;
            mouseX = x;
            mouseY = y;
            Invalidate();
        }

        public void Invalidate()
        {
            _currentZ = 0;
            mouseOverExpressions.Clear();
            mouseOverNull = false;
            InvalidateVisual();
        }

        StupidMouseOverThing? GetIntersectedBlock()
        {
            var ordered = mouseOverExpressions.OrderByDescending(x => x.ZIndex).ToList();
            var expression = ordered.FirstOrDefault(x => x.Expression is ValueExpression);
            expression ??= ordered.FirstOrDefault(x => x.Expression is TypecheckExpression);
            expression ??= ordered.FirstOrDefault(x => x.Expression is EqualsExpression);
            expression ??= mouseOverExpressions.FirstOrDefault();
            return expression;
        }

        private void DebugWindow_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseOverExpressions.Count == 0) return;
            var expression = GetIntersectedBlock();
            if (expression is null) return;
            var name = expression.Expression switch
            {
                EqualsExpression ex => $"{ex.Left} is equal to {ex.Right}",
                ValueExpression ex => ex.Value.ToString(),
                NotExpression ex => $"not {ex.Expression}",
                TypecheckExpression ex => $"{ex.Left} is of type {ex.Right}",
                AndExpression ex => $"{ex.Left} and {ex.Right}",
                _ => "Unknown"
            };
            ctxMenu.ContextMenuItems = new();
            if (mouseOverNull)
            {
                switch (expression.Expression)
                {
                    case TypecheckExpression exp:
                    {
                        ctxMenu.ContextMenuItems.Add(new()
                        {
                            Header = "Add type",
                        });
                        break;
                    }

                    default:
                    {
                        var types = typeof(IExpression).Assembly.GetTypes().Where(x => x.Namespace == "Aerobool.Expressions" && x.GetInterfaces().Contains(typeof(IExpression))).ToList();
                        ctxMenu.ContextMenuItems.Add(new()
                        {
                            Header = "Add condition",
                            SubMenuItems = types.Select(x => new InteropMenuItem()
                            {
                                Header = x.Name,
                                Command = new RelayCommand(() =>
                                {
                                    // use reflection to determine if this has a constructor that takes two IExpression arguments or one
                                    var constructor = x.GetConstructors().FirstOrDefault(y => y.GetParameters().Length == 2 && y.GetParameters().All(z => z.ParameterType == typeof(IExpression)));
                                    if (constructor != null)
                                    {
                                        var instance = constructor.Invoke(new object[] { null, null });
                                        var parent = FindParentExpression(this.expression, expression.Expression);
                                        if (parent is null) return;
                                        var properties = parent.GetType().GetProperties().Where(y => y.PropertyType == typeof(IExpression)).ToList();
                                        var property = properties.FirstOrDefault(y => y.GetValue(parent) == expression.Expression);
                                        if (property is null) return;
                                        property.SetValue(parent, instance);
                                        Invalidate();
                                    } else
                                    {
                                        var instance = Activator.CreateInstance(x);
                                        var parent = FindParentExpression(this.expression, expression.Expression);
                                        if (parent is null) return;
                                        var properties = parent.GetType().GetProperties().Where(y => y.PropertyType == typeof(IExpression)).ToList();
                                        var property = properties.FirstOrDefault(y => y.GetValue(parent) == expression.Expression);
                                        if (property is null) return;
                                        property.SetValue(parent, instance);
                                        Invalidate();
                                    }
                                })
                            }).ToList()
                        });
                        break;
                    }
                }
                ctxMenu.Open();
                return;
            }
            switch (expression.Expression)
            {
                case ValueExpression exp:
                {
                    ctxMenu.ContextMenuItems.Add(new()
                    {
                        Header = "Set value",
                    });
                    break;
                }
            }
            ctxMenu.ContextMenuItems.Add(new()
            {
                Header = "Delete",
                Command = new RelayCommand(() =>
                {
                    var parent = FindParentExpression(this.expression, expression.Expression);
                    if (parent is null) return;
                    var properties = parent.GetType().GetProperties().Where(x => x.PropertyType == typeof(IExpression)).ToList();
                    var property = properties.FirstOrDefault(x => x.GetValue(parent) == expression.Expression);
                    if (property is null) return;
                    property.SetValue(parent, null);
                    Invalidate();
                })
            });
            ctxMenu.Open();
        }

        private IExpression? FindParentExpression(IExpression? expression, IExpression child)
        {
            if (expression is null) return null;
            if (expression is EqualsExpression eq)
            {
                if (eq.Left == child || eq.Right == child) return expression;
                return FindParentExpression(eq.Left, child) ?? FindParentExpression(eq.Right, child);
            }
            if (expression is NotExpression not)
            {
                if (not.Expression == child) return expression;
                return FindParentExpression(not.Expression, child);
            }
            if (expression is TypecheckExpression tc)
            {
                if (tc.Left == child || tc.Right == child) return expression;
                return FindParentExpression(tc.Left, child) ?? FindParentExpression(tc.Right, child);
            }
            if (expression is AndExpression and)
            {
                if (and.Left == child || and.Right == child) return expression;
                return FindParentExpression(and.Left, child) ?? FindParentExpression(and.Right, child);
            }
            return null;
        }

        private void DebugWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var x = (int)e.GetPosition(this).X;
            var y = (int)e.GetPosition(this).Y;
            if (x != mouseX || y != mouseY)
            {
                mouseX = x;
                mouseY = y;
                Invalidate();
            }
        }

        private int _scrolledX = 0;
        private int _scrolledY = 0;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var width = ActualWidth - (Width - ActualWidth);
            var height = ActualHeight - (Height - ActualHeight);
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            // draw an infinite grid of squares in the background
            var gridBrush = new SolidColorBrush(Color.FromArgb(0x20, 0, 0, 0));
            var squareSize = 64;
            var offsetX = _scrolledX % squareSize;
            var offsetY = _scrolledY % squareSize;

            for (var x = offsetX; x < width; x += squareSize)
            {
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(x, 0, 1, height));
            }
            for (var y = offsetY; y < height; y += squareSize)
            {
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(0, y, width, 1));
            }
            DrawExpression(drawingContext, expression, _scrolledY, _scrolledX);
            if (GetIntersectedBlock() is null)
            {
                Cursor = Cursors.SizeAll;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }

        public Rect DrawExpression(DrawingContext drawingContext, IExpression? expression, int top, int left, bool measureOnly = false, IExpression? parent = null)
        {
            Rect rect = new Rect(left, top, 8, 16);
            if (expression is null)
            {
                var text = "null";
                var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, Brushes.White, 96);
                rect.Width += formattedText.Width + 2;
                rect.Height += 1;
                if (!measureOnly)
                {
                    drawingContext.DrawRectangle(Brushes.Black, null, rect);
                    var absTop = top - 4;
                    //drawingContext.DrawText(formattedText, new Point(left, absTop));
                    // center text horizontally and vertically
                    drawingContext.DrawText(formattedText, new Point(left + (rect.Width - formattedText.Width) / 2, top + (rect.Height - formattedText.Height) / 2));
                }
                if (Rect.Intersect(rect, new Rect(mouseX, mouseY, 0, 0)) != Rect.Empty)
                {
                    mouseOverNull = true;
                }
                return rect;
            }
            switch (expression)
            {
                case EqualsExpression equalsExpression:
                {
                    var leftExpression = equalsExpression.Left;
                    var rightExpression = equalsExpression.Right;
                    var leftRect = DrawExpression(drawingContext, leftExpression, 0, 0, true, expression);
                    leftRect.Width += 4;

                    var text = "is equal to";
                    var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, Foreground, 96);

                    var rightRect = DrawExpression(drawingContext, rightExpression, 0, left + (int)leftRect.Width + (int)formattedText.Width + 4, true, expression);

                    var totalRect = new Rect(left, top, leftRect.Width + rightRect.Width + (int)formattedText.Width, Math.Max(Math.Max(leftRect.Height, formattedText.Height), rightRect.Height));

                    totalRect.Width += 4;

                    if (!measureOnly)
                    {
                        drawingContext.DrawRectangle(Brushes.Orange, null, totalRect);

                        DrawExpression(drawingContext, leftExpression, top, left, false, expression);
                        DrawExpression(drawingContext, rightExpression, top, left + (int)leftRect.Width + (int)formattedText.Width + 4, false, expression);
                        drawingContext.DrawText(formattedText, new Point(left + leftRect.Width, top));
                    }

                    rect = totalRect;
                    break;
                }

                case ValueExpression valueExpression:
                {
                    var formattedText = new FormattedText(valueExpression.Value.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, Foreground, 96);
                    var horizontalPadding = 4;
                    if (!measureOnly)
                    {
                        drawingContext.DrawRectangle(Brushes.Cyan, null, new(left, top, formattedText.Width + horizontalPadding * 2, formattedText.Height));
                        drawingContext.DrawText(formattedText, new Point(left + 4, top));
                    }
                    rect = new(left, top, formattedText.Width + horizontalPadding * 2, formattedText.Height);
                    break;
                }

                case NotExpression notExpression:
                {
                    // red background, "not" text, then the expression
                    var text = "not";
                    var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, Brushes.White, 96);
                    var expressionRect = DrawExpression(drawingContext, notExpression.Expression, 0, left + (int)formattedText.Width + 4, true, expression);
                    var totalRect = new Rect(left, top, (int)formattedText.Width + 4 + (int)expressionRect.Width, Math.Max(formattedText.Height, expressionRect.Height));
                    totalRect.Width += 8;
                    totalRect.Height += 4;
                    if (!measureOnly)
                    {
                        drawingContext.DrawRectangle(Brushes.Red, null, totalRect);
                        drawingContext.DrawText(formattedText, new Point(left + 4, top + ((totalRect.Height - formattedText.Height) / 2)));
                        DrawExpression(drawingContext, notExpression.Expression, top + 2, left + (int)formattedText.Width + 8, false, expression);
                    }
                    rect = totalRect;
                    break;
                }

                case TypecheckExpression typecheckExpression:
                {
                    var leftExpression = typecheckExpression.Left;
                    var rightExpression = typecheckExpression.Right;
                    var leftRect = DrawExpression(drawingContext, leftExpression, 0, 0, true, expression);
                    leftRect.Width += 4;

                    var text = "is of type";
                    var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, Brushes.White, 96);

                    var rightRect = DrawExpression(drawingContext, rightExpression, 0, left + (int)leftRect.Width + (int)formattedText.Width + 4, true, expression);

                    var totalRect = new Rect(left, top, leftRect.Width + rightRect.Width + (int)formattedText.Width, Math.Max(Math.Max(leftRect.Height, formattedText.Height), rightRect.Height));

                    totalRect.Width += 4;

                    if (!measureOnly)
                    {
                        drawingContext.DrawRectangle(Brushes.Purple, null, totalRect);

                        DrawExpression(drawingContext, leftExpression, top, left, false, expression);
                        DrawExpression(drawingContext, rightExpression, top, left + (int)leftRect.Width + (int)formattedText.Width + 4, false, expression);
                        drawingContext.DrawText(formattedText, new Point(left + leftRect.Width, top));
                    }

                    rect = totalRect;
                    break;
                }
                case AndExpression andExpression:
                {
                    var padding = 4;

                    var leftExpression = andExpression.Left;
                    var rightExpression = andExpression.Right;

                    var leftRect = DrawExpression(drawingContext, leftExpression, 0, 0, true, expression);
                    leftRect.Width += 4;

                    var text = "and";
                    var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, Brushes.White, 96);

                    var rightRect = DrawExpression(drawingContext, rightExpression, 0, 0, true, expression);

                    var maxHeight = Math.Max(Math.Max(leftRect.Height, formattedText.Height), rightRect.Height);

                    var totalRect = new Rect(left, top, leftRect.Width + rightRect.Width + (int)formattedText.Width + padding * 2,
                                             maxHeight + padding * 2);

                    totalRect.Width += 6;

                    if (!measureOnly)
                    {
                        var pen = new Pen(Brushes.Lime, 1);
                        drawingContext.DrawRectangle(Brushes.Green, pen, totalRect);

                        var leftVerticalOffset = (int)(maxHeight - leftRect.Height) / 2;
                        var rightVerticalOffset = (int)(maxHeight - rightRect.Height) / 2;
                        var textVerticalOffset = (maxHeight - formattedText.Height) / 2;

                        DrawExpression(drawingContext, leftExpression, top + padding + leftVerticalOffset, left + padding, false, expression);
                        DrawExpression(drawingContext, rightExpression, top + padding + rightVerticalOffset, left + padding + (int)leftRect.Width + (int)formattedText.Width + 4, false, expression);
                        drawingContext.DrawText(formattedText, new Point(left + padding + leftRect.Width, top + padding + textVerticalOffset));
                    }

                    rect = totalRect;
                    break;
                }
            }
            var clonedRect = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
            _currentZ++;
            if (Rect.Intersect(clonedRect, new Rect(mouseX, mouseY, 0, 0)) != Rect.Empty)
            {
                var name = expression switch
                {
                    EqualsExpression ex => $"{ex.Left} is equal to {ex.Right}",
                    ValueExpression ex => ex.Value.ToString(),
                    NotExpression ex => $"not {ex.Expression}",
                    TypecheckExpression ex => $"{ex.Left} is of type {ex.Right}",
                    AndExpression ex => $"{ex.Left} and {ex.Right}",
                    _ => "Unknown"
                };
                mouseOverExpressions.Add(new(expression, _currentZ));
            }
            return rect;
        }
    }
}
