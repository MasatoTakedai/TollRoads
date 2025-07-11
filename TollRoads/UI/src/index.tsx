import { ModRegistrar } from "cs2/modding";
import ModIconButton from "./mods/ModIconButton";

const register: ModRegistrar = (moduleRegistry) => {

    moduleRegistry.append('GameTopLeft', ModIconButton);
}

export default register;