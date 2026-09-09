using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Components
{
    public class HexagonDrawable : IDrawable
    {
        public Color StrokeColor { get; set; } = Colors.White;
        public Color FillColor { get; set; } = Colors.Transparent;
        public float StrokeWidth { get; set; } = 2;
        public float CornerRadius { get; set; } = 6;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float width = dirtyRect.Width;
            float height = dirtyRect.Height;

            float centerX = width / 2f;

            // Posições dos 6 vértices
            float topY = 2;
            float upperY = height * 0.25f;
            float lowerY = height * 0.75f;
            float bottomY = height - 2;

            var path = new PathF();

            // Bico superior
            path.MoveTo(centerX, topY);

            // Superior direito
            path.LineTo(width - 4, upperY);

            // Inferior direito
            path.LineTo(width - 4, lowerY);

            // Bico inferior
            path.LineTo(centerX, bottomY);

            // Inferior esquerdo
            path.LineTo(4, lowerY);

            // Superior esquerdo
            path.LineTo(4, upperY);

            path.Close();

            // Preenchimento
            canvas.FillColor = FillColor;
            canvas.FillPath(path);

            // Borda
            canvas.StrokeColor = StrokeColor;
            canvas.StrokeSize = StrokeWidth;
            canvas.StrokeLineJoin = LineJoin.Round;

            canvas.DrawPath(path);
        }
    }
}
