export const Routes = {
  Home: "/",
  About: "/about",
  Portfolio: "/portfolio",
  Contact: "/contact",
  Explore: "/explore",
} as const;

export const GuidedRoutes = [Routes.About, Routes.Portfolio, Routes.Contact, Routes.Explore] as const;
