<%@ Page Language="C#" AutoEventWireup="true" %>
<!DOCTYPE html>
<html>
<head>
    <title>CrossGraphics Clock SVG Sample</title>
</head>
<body>
    <form id="form1" runat="server">

    <%
        Func<string> GetClockSvg = delegate {
            var rect = new System.Drawing.RectangleF (0, 0, 400, 400);
            var clock = new Clock.Clock ();
            using (var w = new System.IO.StringWriter ()) {
                var g = new CrossGraphics.Svg.SvgGraphics (w, rect) {
                    IncludeXmlAndDoctype = false,
                };
                g.BeginDrawing ();
                clock.Width = rect.Width;
                clock.Height = rect.Height;
                clock.Draw (g);
                g.EndDrawing ();
                return w.ToString ();
            }
        };
    %>

    <%=GetClockSvg() %>

    <script>
    setInterval(function () { window.location.reload(false); }, 1000);
    </script>

    </form>
</body>
</html>
