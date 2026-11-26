using System;

namespace MusicScoreManager.Services;

/// <summary>
/// Calculs géométriques purs pour le positionnement des annotations sur les partitions
/// (mode 1 page portrait, mode 2 pages paysage, marges libres et détection de collision de la gomme).
/// </summary>
public static class AnnotationGeometryHelper
{
    public readonly record struct CanvasBounds(double XRel, double YRel, double WRel, double HRel);

    public static CanvasBounds ComputeCanvasBounds(double containerW, double containerH, int imgW, int imgH)
    {
        if (containerW <= 0 || containerH <= 0 || imgW <= 0 || imgH <= 0)
        {
            return new CanvasBounds(0, 0, 1.0, 1.0);
        }

        double imgAspect = (double)imgW / imgH;
        double screenAspect = containerW / containerH;

        if (imgAspect > screenAspect)
        {
            double fitH = containerW / imgAspect;
            double yRel = Math.Max(0, ((containerH - fitH) / 2.0) / containerH);
            double hRel = Math.Min(1.0, fitH / containerH);
            return new CanvasBounds(0, yRel, 1.0, hRel);
        }
        else
        {
            double fitW = containerH * imgAspect;
            double xRel = Math.Max(0, ((containerW - fitW) / 2.0) / containerW);
            double wRel = Math.Min(1.0, fitW / containerW);
            return new CanvasBounds(xRel, 0, wRel, 1.0);
        }
    }

    public static (double x, double y) PageToScreen(
        double rx, double ry, int pageNumber,
        bool isTwoPagesMode, int rightPageNumber,
        CanvasBounds bounds,
        double containerW, double containerH)
    {
        double screenX;
        double screenY;
        if (isTwoPagesMode)
        {
            double pageWRel = bounds.WRel / 2.0;
            double pageHRel = bounds.HRel;
            double pageXStartRel = (pageNumber == rightPageNumber)
                ? (bounds.XRel + pageWRel)
                : bounds.XRel;

            screenX = (pageXStartRel + rx * pageWRel) * containerW;
            screenY = (bounds.YRel + ry * pageHRel) * containerH;
        }
        else
        {
            screenX = (bounds.XRel + rx * bounds.WRel) * containerW;
            screenY = (bounds.YRel + ry * bounds.HRel) * containerH;
        }

        if (containerW > 8) screenX = Math.Clamp(screenX, 4.0, containerW - 4.0);
        if (containerH > 8) screenY = Math.Clamp(screenY, 4.0, containerH - 4.0);
        return (screenX, screenY);
    }

    public static (int pageNumber, double rx, double ry) ScreenToPage(
        double absX, double absY,
        bool isTwoPagesMode, int currentPage, int leftPageNumber, int rightPageNumber, bool hasRightPage,
        CanvasBounds bounds,
        double containerW, double containerH)
    {
        if (containerW <= 0 || containerH <= 0) return (currentPage, 0.5, 0.5);

        double relX = Math.Clamp(absX / containerW, 0.0, 1.0);
        double relY = Math.Clamp(absY / containerH, 0.0, 1.0);

        if (isTwoPagesMode)
        {
            double pageWRel = bounds.WRel / 2.0;
            double midXRel = bounds.XRel + pageWRel;

            if (hasRightPage && relX >= midXRel)
            {
                double rx = pageWRel > 0 ? (relX - midXRel) / pageWRel : 0.0;
                double ry = bounds.HRel > 0 ? (relY - bounds.YRel) / bounds.HRel : 0.0;
                return (rightPageNumber, Math.Clamp(rx, 0.0, 1.0), Math.Clamp(ry, -0.5, 1.5));
            }
            else
            {
                double rx = pageWRel > 0 ? (relX - bounds.XRel) / pageWRel : 0.0;
                double ry = bounds.HRel > 0 ? (relY - bounds.YRel) / bounds.HRel : 0.0;
                return (leftPageNumber, Math.Clamp(rx, 0.0, 1.0), Math.Clamp(ry, -0.5, 1.5));
            }
        }
        else
        {
            double rx = bounds.WRel > 0 ? (relX - bounds.XRel) / bounds.WRel : 0.0;
            double ry = bounds.HRel > 0 ? (relY - bounds.YRel) / bounds.HRel : 0.0;
            return (currentPage, Math.Clamp(rx, -0.25, 1.25), Math.Clamp(ry, -0.5, 1.5));
        }
    }

    public static (double rx, double ry) ScreenToPageCoords(
        double absX, double absY, int targetPage,
        bool isTwoPagesMode, int rightPageNumber,
        CanvasBounds bounds,
        double containerW, double containerH)
    {
        if (containerW <= 0 || containerH <= 0) return (0.5, 0.5);

        double relX = Math.Clamp(absX / containerW, 0.0, 1.0);
        double relY = Math.Clamp(absY / containerH, 0.0, 1.0);

        if (isTwoPagesMode)
        {
            double pageWRel = bounds.WRel / 2.0;
            double pageXStartRel = (targetPage == rightPageNumber)
                ? (bounds.XRel + pageWRel)
                : bounds.XRel;

            double rx = pageWRel > 0 ? (relX - pageXStartRel) / pageWRel : 0.0;
            double ry = bounds.HRel > 0 ? (relY - bounds.YRel) / bounds.HRel : 0.0;
            return (Math.Clamp(rx, 0.0, 1.0), Math.Clamp(ry, -0.5, 1.5));
        }
        else
        {
            double rx = bounds.WRel > 0 ? (relX - bounds.XRel) / bounds.WRel : 0.0;
            double ry = bounds.HRel > 0 ? (relY - bounds.YRel) / bounds.HRel : 0.0;
            return (Math.Clamp(rx, -0.25, 1.25), Math.Clamp(ry, -0.5, 1.5));
        }
    }

    public static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        double lenSq = dx * dx + dy * dy;
        if (lenSq <= 0.0001)
        {
            return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        }
        double t = Math.Clamp(((px - x1) * dx + (py - y1) * dy) / lenSq, 0.0, 1.0);
        double projX = x1 + t * dx;
        double projY = y1 + t * dy;
        return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }
}
