import * as React from "react";

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { IconDefinition } from "@fortawesome/fontawesome-svg-core";

import cn from "classnames";

import "./ToolbarButton.css";

type Props = {
  icon: IconDefinition;
  isActive?: boolean;
  onClick: () => void;
};

export const ToolbarButton: React.FC<Props> = ({ icon, onClick, isActive }) => {
  return (
    <button
      className={cn("toolbarButton", isActive && "active")}
      onMouseDown={(e) => {
        // prevent switching focus to button
        e.preventDefault();
        onClick();
      }}
    >
      <FontAwesomeIcon icon={icon} />
    </button>
  );
};
