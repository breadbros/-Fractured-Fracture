// O3 produces literally 1/3 the instructions of OD or O0 so let's just be kind to the driver
#pragma fxcparams(/O3 /Zi)

#define Textured false
#include "RasterStrokePolygonImpl.fxh"

technique RasterStrokePolygon
{
    pass P0
    {
        vertexShader = compile vs_3_0 RasterStrokePolygonVertexShader();
        pixelShader = compile ps_3_0 RasterStrokePolygonFragmentShader();
    }
}
