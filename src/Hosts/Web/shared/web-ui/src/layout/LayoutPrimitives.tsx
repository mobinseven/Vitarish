import {
  forwardRef,
  type ComponentPropsWithoutRef,
  type HTMLAttributes,
  type ReactNode,
} from "react";

export const pageWidths = ["compact", "reading", "content", "fluid"] as const;

export type PageWidth = (typeof pageWidths)[number];

export interface PublicDocumentShellProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  footer?: ReactNode;
  header?: ReactNode;
}

export const PublicDocumentShell = forwardRef<HTMLDivElement, PublicDocumentShellProps>(
  function PublicDocumentShell({ children, className, footer, header, ...props }, ref) {
    return (
      <div
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-public-document-shell", className)}
        data-vitarish-layout="public-document-shell"
        data-vitarish-scroll-owner="document"
      >
        {header ? (
          <header data-vitarish-layout-slot="header">{header}</header>
        ) : null}
        {children}
        {footer ? (
          <footer data-vitarish-layout-slot="footer">{footer}</footer>
        ) : null}
      </div>
    );
  }
);

export interface PageContainerProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  width?: PageWidth;
}

export const PageContainer = forwardRef<HTMLDivElement, PageContainerProps>(
  function PageContainer({ children, className, width = "content", ...props }, ref) {
    return (
      <div
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-page-container", className)}
        data-vitarish-layout="page-container"
        data-vitarish-page-width={width}
      >
        {children}
      </div>
    );
  }
);

export interface ContainedPageProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  width?: PageWidth;
}

export const ContainedPage = forwardRef<HTMLDivElement, ContainedPageProps>(
  function ContainedPage({ children, className, width = "content", ...props }, ref) {
    return (
      <div
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-contained-page", className)}
        data-vitarish-layout="contained-page"
        data-vitarish-page-width={width}
      >
        <PageContainer width={width}>{children}</PageContainer>
      </div>
    );
  }
);

export interface CenteredRegionProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

export const CenteredRegion = forwardRef<HTMLDivElement, CenteredRegionProps>(
  function CenteredRegion({ children, className, ...props }, ref) {
    return (
      <div
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-centered-region", className)}
        data-vitarish-layout="centered-region"
      >
        {children}
      </div>
    );
  }
);

export interface ImmersiveRegionProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

export const ImmersiveRegion = forwardRef<HTMLDivElement, ImmersiveRegionProps>(
  function ImmersiveRegion({ children, className, ...props }, ref) {
    return (
      <div
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-immersive-region", className)}
        data-vitarish-layout="immersive-region"
        data-vitarish-scroll-owner="workspace"
      >
        {children}
      </div>
    );
  }
);

export interface StandaloneDocumentPageProps extends ComponentPropsWithoutRef<"main"> {
  children: ReactNode;
}

export const StandaloneDocumentPage = forwardRef<HTMLElement, StandaloneDocumentPageProps>(
  function StandaloneDocumentPage({ children, className, ...props }, ref) {
    return (
      <main
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-standalone-document-page", className)}
        data-vitarish-layout="standalone-document-page"
        data-vitarish-scroll-owner="document"
      >
        {children}
      </main>
    );
  }
);

export interface StandaloneCenteredPageProps extends ComponentPropsWithoutRef<"main"> {
  children: ReactNode;
  width?: PageWidth;
}

export const StandaloneCenteredPage = forwardRef<HTMLElement, StandaloneCenteredPageProps>(
  function StandaloneCenteredPage(
    { children, className, width = "compact", ...props },
    ref
  ) {
    return (
      <main
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-standalone-centered-page", className)}
        data-vitarish-layout="standalone-centered-page"
        data-vitarish-scroll-owner="document"
      >
        <PageContainer width={width}>{children}</PageContainer>
      </main>
    );
  }
);

export interface StandaloneImmersivePageProps extends ComponentPropsWithoutRef<"main"> {
  children: ReactNode;
}

export const StandaloneImmersivePage = forwardRef<HTMLElement, StandaloneImmersivePageProps>(
  function StandaloneImmersivePage({ children, className, ...props }, ref) {
    return (
      <main
        {...props}
        ref={ref}
        className={mergeClassNames("vitarish-standalone-immersive-page", className)}
        data-vitarish-layout="standalone-immersive-page"
        data-vitarish-scroll-owner="workspace"
      >
        {children}
      </main>
    );
  }
);

function mergeClassNames(...classNames: Array<string | undefined>) {
  const result = classNames.filter(Boolean).join(" ");
  return result || undefined;
}
