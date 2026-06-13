import { ChevronLeft, ChevronRight, SkipBack } from "lucide-react";

type BlockNavigatorProps = {
  blockCount: number;
  currentBlock: number;
  disabled: boolean;
  showEmpty: boolean;
  onFirst: () => void;
  onPrevious: () => void;
  onNext: () => void;
};

export function BlockNavigator({
  blockCount,
  currentBlock,
  disabled,
  showEmpty,
  onFirst,
  onPrevious,
  onNext
}: BlockNavigatorProps) {
  if (blockCount === 0 && !showEmpty) {
    return null;
  }

  const hasBlocks = blockCount > 0;
  const controlsDisabled = disabled || !hasBlocks;

  return (
    <section className="block-navigator" aria-label="Block navigation">
      <button type="button" disabled={controlsDisabled} onClick={onFirst} title="First block">
        <SkipBack size={16} aria-hidden="true" />
        <span>First Block</span>
      </button>
      <button type="button" disabled={controlsDisabled || currentBlock <= 0} onClick={onPrevious} title="Previous block">
        <ChevronLeft size={16} aria-hidden="true" />
        <span>Prev Block</span>
      </button>
      <button type="button" disabled={controlsDisabled || currentBlock >= blockCount - 1} onClick={onNext} title="Next block">
        <ChevronRight size={16} aria-hidden="true" />
        <span>Next Block</span>
      </button>
      <div className="block-count">
        {hasBlocks ? `Loaded ${blockCount} blocks - Block ${Math.min(currentBlock + 1, blockCount)} of ${blockCount}` : "No blocks"}
      </div>
    </section>
  );
}
