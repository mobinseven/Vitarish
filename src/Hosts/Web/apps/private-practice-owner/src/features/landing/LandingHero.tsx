import Image from "next/image";
import { ImmersiveRegion } from "@vitarish/ui";

export function LandingHero() {
  return (
    <ImmersiveRegion className="relative min-h-[42rem] w-full overflow-hidden">
      <Image
        src="/owner-hero.jpg"
        alt="Modern private practice interior"
        fill
        priority
        sizes="100vw"
        className="object-cover object-center"
      />
      <div className="absolute inset-0 bg-black/10" />
    </ImmersiveRegion>
  );
}
