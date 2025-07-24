import { ModRegistrar } from "cs2/modding";
import TollRoadsButton from "./mods/TollRoadsButton";
import TollRoadsMenu from "./mods/TollRoadsMenu/TollRoadsMenu";

const register: ModRegistrar = (moduleRegistry) => {

    moduleRegistry.append('GameTopLeft', TollRoadsButton);
    moduleRegistry.append('Game', TollRoadsMenu);
}

export default register;