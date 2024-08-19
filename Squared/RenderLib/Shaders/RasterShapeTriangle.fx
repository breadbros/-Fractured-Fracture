// O3 produces literally 1/3 the instructions of OD or O0 so let's just be kind to the driver
#pragma fxcparams(/O3 /Zi)

#include "RasterShapeConstants.fxh"

#define SHAPE_TYPE_NAME RasterTriangleUntextured
#define SHAPE_TYPE_NAME_TEX RasterTriangleTextured
#define SHAPE_TYPE_TECHNIQUE_NAME RasterTriangleTechnique
#define SHAPE_TYPE_TECHNIQUE_NAME_TEX TexturedRasterTriangleTechnique
#define SHAPE_TYPE_NAME_SHADOWED ShadowedRasterTriangleUntextured
#define SHAPE_TYPE_NAME_TEX_SHADOWED ShadowedRasterTriangleTextured
#define SHAPE_TYPE_TECHNIQUE_NAME_SHADOWED ShadowedRasterTriangleTechnique
#define SHAPE_TYPE_TECHNIQUE_NAME_TEX_SHADOWED ShadowedTexturedRasterTriangleTechnique
#define SHAPE_TYPE_NAME_SIMPLE RasterTriangleSimple
#define SHAPE_TYPE_TECHNIQUE_NAME_SIMPLE RasterTriangleSimpleTechnique
#define SHAPE_TYPE_NAME_SIMPLE_SHADOWED ShadowedRasterTriangleSimple
#define SHAPE_TYPE_TECHNIQUE_NAME_SIMPLE_SHADOWED ShadowedRasterTriangleSimpleTechnique
#define SHAPE_TYPE_NAME_RAMP RasterTriangleRamp
#define SHAPE_TYPE_TECHNIQUE_NAME_RAMP RasterTriangleRampTechnique
#define SHAPE_TYPE_NAME_RAMP_SHADOWED ShadowedRasterTriangleRamp
#define SHAPE_TYPE_TECHNIQUE_NAME_RAMP_SHADOWED ShadowedRasterTriangleRampTechnique

#define INCLUDE_TRIANGLE
#define EVALUATE_TYPE TYPE_Triangle

#include "RasterShapeSkeleton.fxh"
#include "RasterShapeImpl.fxh"