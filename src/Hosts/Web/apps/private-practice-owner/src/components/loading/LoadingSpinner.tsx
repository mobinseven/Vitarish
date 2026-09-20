"use client";

export function LoadingSpinner({ size = 128, className }: { size?: number; className?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 320 320" xmlns="http://www.w3.org/2000/svg" className={className} aria-hidden="true">
      <defs>
        <linearGradient id="g1" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="#FFE3C8" /><stop offset="100%" stopColor="#FFB3CE" /></linearGradient>
        <linearGradient id="g2" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="#FFC59D" /><stop offset="100%" stopColor="#FF7CAA" /></linearGradient>
        <linearGradient id="g3" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="#FFAE82" /><stop offset="100%" stopColor="#FF5F90" /></linearGradient>
      </defs>
      <style>{`
        .ring1 { transform-origin: 160px 160px; animation: spin1 7.3s linear infinite, opacity1 10s ease-in-out infinite; }
        .ring2 { transform-origin: 160px 160px; animation: spin2 8.5s linear infinite, opacity2 10s ease-in-out infinite; }
        .ring3 { transform-origin: 160px 160px; animation: spin3 11s linear infinite, opacity3 10s ease-in-out infinite; }
        @keyframes spin1 { to { transform: rotate(360deg); } }
        @keyframes spin2 { to { transform: rotate(-360deg); } }
        @keyframes spin3 { to { transform: rotate(360deg); } }
        @keyframes opacity1 { 0% { opacity: .18; } 13% { opacity: .52; } 27% { opacity: .31; } 43% { opacity: .68; } 61% { opacity: .24; } 78% { opacity: .57; } 91% { opacity: .36; } 100% { opacity: .18; } }
        @keyframes opacity2 { 0% { opacity: .54; } 17% { opacity: .27; } 32% { opacity: .63; } 48% { opacity: .19; } 64% { opacity: .47; } 81% { opacity: .72; } 100% { opacity: .54; } }
        @keyframes opacity3 { 0% { opacity: .30; } 11% { opacity: .69; } 29% { opacity: .22; } 46% { opacity: .51; } 59% { opacity: .34; } 76% { opacity: .76; } 88% { opacity: .42; } 100% { opacity: .30; } }
        @media (prefers-reduced-motion: reduce) { .ring1, .ring2, .ring3 { animation: none; } }
      `}</style>
      <g className="ring1"><path d="M 288.82 160.00 C 288.87 178.19 282.86 199.51 273.46 214.64 C 264.05 229.76 246.93 240.79 232.38 250.76 C 217.82 260.73 202.59 270.28 186.12 274.44 C 169.65 278.61 149.59 280.22 133.58 275.77 C 117.56 271.32 103.25 258.54 90.04 247.72 C 76.83 236.91 63.65 225.51 54.32 210.89 C 44.99 196.27 35.92 177.85 34.08 160.00 C 32.24 142.15 35.63 120.53 43.27 103.78 C 50.90 87.04 64.62 68.51 79.89 59.55 C 95.16 50.58 117.67 50.24 134.89 50.01 C 152.12 49.77 166.94 55.01 183.25 58.14 C 199.56 61.28 217.75 60.89 232.74 68.79 C 247.72 76.68 263.82 90.30 273.16 105.50 C 282.51 120.71 288.77 141.81 288.82 160.00 Z" fill="url(#g1)" /></g>
      <g className="ring2"><path d="M 275.94 160.00 C 275.98 176.37 270.58 195.56 262.11 209.17 C 253.64 222.79 238.24 232.71 225.14 241.68 C 212.04 250.65 198.33 259.25 183.51 263.00 C 168.69 266.75 150.63 268.20 136.22 264.19 C 121.81 260.18 108.93 248.68 97.04 238.95 C 85.15 229.22 73.28 218.96 64.89 205.80 C 56.49 192.64 48.33 176.07 46.67 160.00 C 45.01 143.93 48.07 124.47 54.94 109.41 C 61.81 94.34 74.16 77.66 87.90 69.59 C 101.65 61.52 121.90 61.22 137.41 61.00 C 152.91 60.79 166.25 65.51 180.92 68.33 C 195.60 71.15 211.98 70.81 225.47 77.91 C 238.95 85.01 253.43 97.27 261.85 110.95 C 270.26 124.64 275.90 143.63 275.94 160.00 Z" fill="url(#g2)" /></g>
      <g className="ring3"><path d="M 263.06 160.00 C 263.10 174.55 258.29 191.61 250.76 203.71 C 243.24 215.81 229.55 224.63 217.90 232.61 C 206.26 240.58 194.07 248.22 180.90 251.56 C 167.72 254.89 151.67 256.18 138.86 252.62 C 126.05 249.05 114.60 238.83 104.03 230.18 C 93.47 221.53 82.92 212.41 75.46 200.71 C 68.00 189.02 60.74 174.28 59.26 160.00 C 57.79 145.72 60.51 128.42 66.61 115.03 C 72.72 101.63 83.70 86.81 95.91 79.64 C 108.13 72.47 126.13 72.19 139.92 72.00 C 153.70 71.82 165.55 76.01 178.60 78.52 C 191.64 81.02 206.20 80.72 218.19 87.03 C 230.18 93.34 243.05 104.24 250.53 116.40 C 258.01 128.56 263.02 145.45 263.06 160.00 Z" fill="url(#g3)" /></g>
    </svg>
  );
}
