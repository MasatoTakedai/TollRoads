import { bindValue, trigger } from "cs2/api";
import { TollRoadItem } from "domain/TollRoadItem"
import mod from "mod.json";

export const tollRoadsToolEnabled$ = bindValue<boolean>(mod.id, "TollRoadsToolEnabled", false);
export const tollRoads$ = bindValue<TollRoadItem[]>(mod.id, "GetTollRoads", []);

export const toggleTool = trigger.bind(null, mod.id, "ToggleTool");