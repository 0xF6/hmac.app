namespace hmac.Core
{
    using System;
    using System.Dynamic;
    using System.Numerics;
    using System.Threading.Tasks;
    using Microsoft.JSInterop;

    public class ToastController : IToastController
    {
        private readonly IJSRuntime _js;

        public ToastController(IJSRuntime js) => _js = js;

        public IToast Open() => new Toast(this);


        private ValueTask InvokeAsync(object obj)
            => _js.InvokeVoidAsync("__toast", obj);


        protected class Toast : IToast
        {
            private readonly ToastController _controller;
            private dynamic bag { get; }

            public Toast(ToastController controller)
            {
                _controller = controller;
                bag = new ExpandoObject();
            }


            private string TransformVector(Vector2 vector2)
            {
                var first = "";
                var last = "";
                if (vector2.X >= 1)
                    first = "top";
                if (vector2.X <= -1)
                    first = "right";

                if (vector2.Y >= 1)
                    last = "top";
                if (vector2.Y <= -1)
                    last = "left";

                return $"{first} {last}";
            }

            private IToast Actor(Action action)
            {
                action();
                return this;
            }

            #region Implementation of IToast

            public IToast WithMessage(string message)
                => Actor(() => bag.message = message);

            public IToast WithTitle(string title)
                => Actor(() => bag.title = title);

            public IToast WithPosition(Vector2 vector)
                => Actor(() => bag.position = TransformVector(vector));

            public IToast WithType(ToastType type)
                => Actor(() => bag.@class = type.ToString().ToLowerInvariant());

            /// <summary>
            /// 
            /// </summary>
            /// <param name="span"></param>
            /// <returns></returns>
            /// <remarks>
            /// when TimeSpan.MaxValue -> set auto
            /// </remarks>
            public IToast WithDuration(TimeSpan? span)
            {
                if (span is null)
                    return Actor(() => bag.displayTime = null);
                if (span.Value == TimeSpan.MaxValue || span.Value == TimeSpan.MinValue)
                    return Actor(() => bag.displayTime = "auto");
                return Actor(() => bag.displayTime = (int) span.Value.TotalMilliseconds);
            }

            public IToast WithProgressDirection(bool isUp)
            {
                if (isUp) bag.progressUp = true;
                return this;
            }

            public ValueTask InvokeAsync()
                => this._controller.InvokeAsync(this.bag);

            #endregion
        }
    }

    public interface IToast
    {
        /// <summary>
        /// A minimal toast will just display a message.
        /// </summary>
        IToast WithMessage(string message);

        /// <summary>
        /// You can add a title to your toast.
        /// </summary>
        IToast WithTitle(string title);

        /// <summary>
        /// A toast can appear in different positions on the screen.
        /// { X 1 -> top, X -1 -> bottom, Y 1 -> right, Y -1 -> left }
        /// </summary>
        IToast WithPosition(Vector2 vector);

        /// <summary>
        /// A toast can be used to display different types of informations.
        /// </summary>
        IToast WithType(ToastType type);

        /// <summary>
        /// You can choose how long a toast should be displayed.
        /// You can even avoid a toast to disapear when set <see cref="TimeSpan.Zero"/>
        /// Setting the value to <see cref="TimeSpan.MaxValue"/> or <see cref="TimeSpan.MinValue"/>
        /// calculates the display time by the amount of containing words
        /// </summary>
        IToast WithDuration(TimeSpan? span);

        /// <summary>
        /// The progress bar could be be raised instead of lowered
        /// </summary>
        IToast WithProgressDirection(bool isUp);

        /// <summary>
        /// Show toast on page.
        /// </summary>
        ValueTask InvokeAsync();
    }

    public enum ToastType
    {
        Success,
        Warning,
        Error
    }

    public interface IToastController
    {
        IToast Open();
    }
}