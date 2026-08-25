import { ItemCard, PressableFeedback } from "@heroui-pro/react";

export interface ImageActionCardProps {
  className?: string;
  imageSrc: string;
  isDisabled?: boolean;
  label: string;
  labelPosition?: "center" | "bottom";
  onPress?: () => void;
}

export function ImageActionCard({
  className,
  imageSrc,
  isDisabled = false,
  label,
  labelPosition = "center",
  onPress,
}: ImageActionCardProps) {
  return (
    <ItemCard<"button">
      className={`relative min-h-40 cursor-pointer overflow-hidden${className ? ` ${className}` : ""}`}
      disabled={isDisabled}
      onClick={onPress}
      render={(props) => <button type="button" {...props} />}
    >
      <img
        alt=""
        aria-hidden="true"
        className="absolute inset-0 size-full scale-[1.06] object-cover object-center"
        src={imageSrc}
      />
      <PressableFeedback.Highlight />
      <ItemCard.Content
        className={
          labelPosition === "bottom"
            ? "absolute inset-0 z-10 items-start justify-end p-6 rtl:items-end"
            : "relative z-10 items-center justify-center"
        }
      >
        <ItemCard.Title
          className={
            labelPosition === "bottom"
              ? "flex flex-col items-start gap-2 text-left text-lg text-white drop-shadow-sm rtl:items-end"
              : "w-fit rounded-xl bg-white px-4 py-2 text-center text-black"
          }
        >
          {label}
        </ItemCard.Title>
      </ItemCard.Content>
    </ItemCard>
  );
}
