namespace Basic.ImGui.Layout
{
    public ref struct ElementScope
    {
        readonly ImGuiContext _context;

        internal ElementScope(ImGuiContext context) => _context = context;

        public void Dispose() => _context.CloseElement();

        public ElementScope Padding(float all)
        {
            _context.OverrideOpenPadding(all, all, all, all);
            return this;
        }

        public ElementScope Padding(float left, float top, float right, float bottom)
        {
            _context.OverrideOpenPadding(left, top, right, bottom);
            return this;
        }

        public ElementScope ChildGap(float gap)
        {
            _context.OverrideOpenChildGap(gap);
            return this;
        }

        public ElementScope OnHover(ElementHoverCallback callback)
        {
            _context.OverrideOpenHover(callback);
            return this;
        }
    }
}
