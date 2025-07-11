import { bindValue, trigger } from "cs2/api";
import mod from "mod.json";

export const tollRoadsToolEnabled$ = bindValue<boolean>(mod.id, "TollRoadsToolEnabled", false);

export const toggleTool = trigger.bind(null, mod.id, "ToggleTool");