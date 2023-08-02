using System.Diagnostics.CodeAnalysis;

namespace Tomat.SharpLink;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public enum HlTypeKind {
    HVOID	  = 0,
    HUI8	  = 1,
    HUI16	  = 2,
    HI32	  = 3,
    HI64	  = 4,
    HF32	  = 5,
    HF64	  = 6,
    HBOOL	  = 7,
    HBYTES	  = 8,
    HDYN	  = 9,
    HFUN	  = 10,
    HOBJ	  = 11,
    HARRAY	  = 12,
    HTYPE	  = 13,
    HREF	  = 14,
    HVIRTUAL  = 15,
    HDYNOBJ   = 16,
    HABSTRACT = 17,
    HENUM	  = 18,
    HNULL	  = 19,
    HMETHOD   = 20,
    HSTRUCT	  = 21,
    HPACKED   = 22,

    HLAST	  = 23,

    // _H_FORCE_INT = 0x7FFFFFFF,
}
