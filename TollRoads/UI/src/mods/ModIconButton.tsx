import { FloatingButton, Tooltip } from "cs2/ui";
import { useValue } from "cs2/api";
import icon from "../img/icon.svg";
import { tollRoadsToolEnabled$, toggleTool } from "mods/bindings";

export default () => {
    const tollRoadsToolEnabled = useValue(tollRoadsToolEnabled$);
    return (
        <Tooltip tooltip="Toll Roads">
            <FloatingButton
                src={icon}
                selected={tollRoadsToolEnabled}
                onSelect={toggleTool}
            />
        </Tooltip>
    );
};