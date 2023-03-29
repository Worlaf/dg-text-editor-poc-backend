import { RenderLeafProps } from "slate-react";

import "./EditorLeaf.css";

export const EditorLeaf: React.FC<RenderLeafProps> = ({
  attributes,
  children,
  leaf,
}) => {
  const style: React.CSSProperties = {
    backgroundColor: leaf.selectionBackgroundColor ?? leaf.backgroundColor,
  };

  if (leaf.isBold) {
    children = <strong style={style}>{children}</strong>;
  }

  if (leaf.isItalic) {
    children = <em style={style}>{children}</em>;
  }

  if (leaf.isStrikethrough) {
    children = <s style={style}>{children}</s>;
  }

  return (
    <span {...attributes} style={style}>
      {children}
      {leaf.selectionEndLabel && (
        <span
          className="selectionLabelContainer"
          style={{ borderRightColor: leaf.selectionBackgroundColor }}
          contentEditable={false}
        >
          <span
            style={{ backgroundColor: leaf.selectionBackgroundColor }}
            className="selectionLabel"
          >
            {leaf.selectionEndLabel}
          </span>
        </span>
      )}
    </span>
  );
};
