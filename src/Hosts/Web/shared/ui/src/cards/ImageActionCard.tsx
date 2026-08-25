import { ItemCard, PressableFeedback } from "@heroui-pro/react";

export interface ImageActionCardProps {
  className?: string;
  imageSrc: string;
  isDisabled?: boolean;
  label: string;
  onPress?: () => void;
}

export function ImageActionCard({
  className,
  imageSrc,
  isDisabled = false,
  label,
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
        className="absolute inset-0 size-full object-cover object-center"
        src={imageSrc}
      />
      <PressableFeedback.Highlight />
      <ItemCard.Content className="relative z-10 items-center justify-center">
        <ItemCard.Title className="w-fit rounded-xl bg-white px-4 py-2 text-center text-black">
          {label}
        </ItemCard.Title>
      </ItemCard.Content>
    </ItemCard>
  );
}
