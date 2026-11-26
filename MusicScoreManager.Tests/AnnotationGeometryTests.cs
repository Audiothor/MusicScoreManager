using MusicScoreManager.Services;
using Xunit;

namespace MusicScoreManager.Tests;

public class AnnotationGeometryTests
{
    [Fact]
    public void ComputeCanvasBounds_PortraitLetterbox_ComputesVerticalMargins()
    {
        // Ecran portrait 800x1600 (ratio 0.5) avec page A4 1000x1414 (ratio ~0.707)
        var bounds = AnnotationGeometryHelper.ComputeCanvasBounds(800, 1600, 1000, 1414);

        Assert.Equal(0.0, bounds.XRel, 4);
        Assert.Equal(1.0, bounds.WRel, 4);
        Assert.True(bounds.YRel > 0.1, "Une page A4 sur écran 1:2 doit avoir une marge verticale.");
        Assert.True(bounds.HRel < 0.8);
        Assert.Equal(1.0, bounds.YRel * 2 + bounds.HRel, 4);
    }

    [Fact]
    public void ComputeCanvasBounds_LandscapePillarbox_ComputesHorizontalMargins()
    {
        // Ecran paysage 1920x1080 (ratio 1.777) avec double page 2000x1414 (ratio 1.414)
        var bounds = AnnotationGeometryHelper.ComputeCanvasBounds(1920, 1080, 2000, 1414);

        Assert.Equal(0.0, bounds.YRel, 4);
        Assert.Equal(1.0, bounds.HRel, 4);
        Assert.True(bounds.XRel > 0.05);
        Assert.Equal(1.0, bounds.XRel * 2 + bounds.WRel, 4);
    }

    [Fact]
    public void ScreenToPage_And_PageToScreen_RoundTrip_SinglePagePortrait()
    {
        var bounds = AnnotationGeometryHelper.ComputeCanvasBounds(800, 1280, 1000, 1414);
        double origX = 320.0;
        double origY = 640.0;

        var (page, rx, ry) = AnnotationGeometryHelper.ScreenToPage(
            origX, origY, isTwoPagesMode: false, currentPage: 3,
            leftPageNumber: 3, rightPageNumber: 4, hasRightPage: false,
            bounds, 800, 1280);

        Assert.Equal(3, page);

        var (sx, sy) = AnnotationGeometryHelper.PageToScreen(
            rx, ry, page, isTwoPagesMode: false, rightPageNumber: 4, bounds, 800, 1280);

        Assert.Equal(origX, sx, 2);
        Assert.Equal(origY, sy, 2);
    }

    [Fact]
    public void ScreenToPage_And_PageToScreen_RoundTrip_TwoPagesLandscape_LeftAndRight()
    {
        var bounds = AnnotationGeometryHelper.ComputeCanvasBounds(1600, 1000, 2000, 1414);

        // Point sur la page de gauche (page 1)
        var (leftPage, lrx, lry) = AnnotationGeometryHelper.ScreenToPage(
            450, 500, isTwoPagesMode: true, currentPage: 1,
            leftPageNumber: 1, rightPageNumber: 2, hasRightPage: true,
            bounds, 1600, 1000);

        Assert.Equal(1, leftPage);
        var (lsx, lsy) = AnnotationGeometryHelper.PageToScreen(
            lrx, lry, leftPage, isTwoPagesMode: true, rightPageNumber: 2, bounds, 1600, 1000);
        Assert.Equal(450, lsx, 2);
        Assert.Equal(500, lsy, 2);

        // Point sur la page de droite (page 2)
        var (rightPage, rrx, rry) = AnnotationGeometryHelper.ScreenToPage(
            1150, 700, isTwoPagesMode: true, currentPage: 1,
            leftPageNumber: 1, rightPageNumber: 2, hasRightPage: true,
            bounds, 1600, 1000);

        Assert.Equal(2, rightPage);
        var (rsx, rsy) = AnnotationGeometryHelper.PageToScreen(
            rrx, rry, rightPage, isTwoPagesMode: true, rightPageNumber: 2, bounds, 1600, 1000);
        Assert.Equal(1150, rsx, 2);
        Assert.Equal(700, rsy, 2);
    }

    [Fact]
    public void OptionC_WhiteMarginPlacement_PreservedInPortrait_ClampedSafelyInLandscape()
    {
        // Portrait avec marge blanche en haut (YRel > 0)
        var portraitBounds = AnnotationGeometryHelper.ComputeCanvasBounds(800, 1600, 1000, 1200);
        var (page, rx, ry) = AnnotationGeometryHelper.ScreenToPage(
            400, 40, isTwoPagesMode: false, currentPage: 1,
            leftPageNumber: 1, rightPageNumber: 2, hasRightPage: false,
            portraitBounds, 800, 1600);

        Assert.Equal(1, page);
        Assert.True(ry < 0.0, "Une annotation placée dans la marge blanche haute doit conserver un ry négatif (Option C).");

        // En portrait, elle revient à sa position exacte Y = 40
        var (_, portraitY) = AnnotationGeometryHelper.PageToScreen(
            rx, ry, page, isTwoPagesMode: false, rightPageNumber: 2, portraitBounds, 800, 1600);
        Assert.Equal(40.0, portraitY, 2);

        // En paysage (YRel == 0), elle est maintenue visible au bord supérieur (>= 4px)
        var landscapeBounds = AnnotationGeometryHelper.ComputeCanvasBounds(1600, 800, 2000, 1200);
        var (_, landscapeY) = AnnotationGeometryHelper.PageToScreen(
            rx, ry, page, isTwoPagesMode: true, rightPageNumber: 2, landscapeBounds, 1600, 800);
        Assert.True(landscapeY >= 4.0 && landscapeY <= 796.0);
    }

    [Fact]
    public void DistanceToSegment_DetectsMidpointAndEndpointsAccurately()
    {
        // Segment horizontal de (0, 100) à (200, 100)
        double dMid = AnnotationGeometryHelper.DistanceToSegment(100, 105, 0, 100, 200, 100);
        Assert.Equal(5.0, dMid, 4);

        // Point au-delà de l'extrémité droite
        double dEnd = AnnotationGeometryHelper.DistanceToSegment(203, 104, 0, 100, 200, 100);
        Assert.Equal(5.0, dEnd, 4);

        // Segment dégénéré (point unique)
        double dDeg = AnnotationGeometryHelper.DistanceToSegment(13, 14, 10, 10, 10, 10);
        Assert.Equal(5.0, dDeg, 4);
    }
}
