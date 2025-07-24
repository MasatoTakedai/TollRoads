import { Bounds1, Color, Theme, UniqueFocusKey } from "cs2/bindings";
import { InputAction } from "cs2/input";
import { ModuleRegistry } from "cs2/modding";
import { BalloonDirection, FocusKey, PanelTheme, ScrollController } from "cs2/ui";
import { CSSProperties, EventHandler, HTMLAttributes, KeyboardEventHandler, MouseEventHandler, MutableRefObject, ReactNode } from "react";

// These are specific to the types of components that this mod uses.
// In the UI developer tools at http://localhost:9444/ go to Sources -> Index.js. Pretty print if it is formatted in a single line.
// Search for the tsx or scss files. Look at the function referenced and then find the properies for the component you're interested in.
// As far as I know the types of properties are just guessed.
type PropsToolButton = {
    focusKey?: UniqueFocusKey | null;
    src: string;
    selected?: boolean;
    multiSelect?: boolean;
    disabled?: boolean;
    tooltip?: string | JSX.Element | null;
    selectSound?: any;
    uiTag?: string;
    className?: string;
    children?: string | JSX.Element | JSX.Element[];
    onSelect?: (x: any) => any;
} & HTMLAttributes<any>;

type PropsSection = {
    title?: string | null;
    uiTag?: string;
    children: string | JSX.Element | JSX.Element[];
};

type ToggleProps = {
    focusKey?: FocusKey;
    checked?: boolean;
    disabled?: boolean;
    style?: CSSProperties;
    className?: string;
    children?: ReactNode;
    onChange?: () => void;
    onMouseOver?: () => void;
    onMouseLeave?: () => void;
};

type Checkbox = {
    checked?: boolean;
    disabled?: boolean;
    className?: string;
    theme?: any;
} & HTMLAttributes<any>;

type DataInput = {
    //idk what this should be named
    value: any;
    valueFormatter: () => string;
    inputValidator: (text: string) => boolean;
    inputTransformer?: (text: string) => string;
    inputParser: (text: string, t: number, n: number) => any;
    onChange?: (text: string) => void;
    onFocus?: (e: any) => void;
    onBlur?: (e: any) => void;
};

type IntInput = {
    min?: number;
    max?: number;
    className: string;
} & Partial<DataInput>;

type BoundIntInputField = IntInput;

type TextInputTheme = {
    input: string;
    label: string;
    container: string;
};

type EllipsisTextInput = {
    value: string | undefined;
    maxLength?: number; // default is 64
    theme?: any;
    className?: string;
    vkTitle?: string;
    placeholder?: string;
    ref?: MutableRefObject<HTMLInputElement>;
    focusKey?: FocusKey;
    onClick?: MouseEventHandler;
    onKeyDown?: KeyboardEventHandler<HTMLInputElement>;
    onChange?: (event: React.ChangeEvent<HTMLInputElement>) => void;
    onFocus?: () => void;
    onBlur?: () => void;
};

type EllipsesTextInputTheme = {
    "ellipses-text-input": string;
    ellipsesTextInput: string;
    input: string;
    label: string;
};

// This is an array of the different components and sass themes that are appropriate for your UI. You need to figure out which ones you need from the registry.
const registryIndex = {
    Section: ["game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx", "Section"],
    ToolButton: ["game-ui/game/components/tool-options/tool-button/tool-button.tsx", "ToolButton"],
    toolButtonTheme: ["game-ui/game/components/tool-options/tool-button/tool-button.module.scss", "classes"],
    Toggle: ["game-ui/common/input/toggle/toggle.tsx", "Toggle"],
    toggleTheme: ["game-ui/menu/widgets/toggle-field/toggle-field.module.scss", "classes"],
    Checkbox: ["game-ui/common/input/toggle/checkbox/checkbox.tsx", "Checkbox"],
    checkboxTheme: ["game-ui/common/input/toggle/checkbox/checkbox.module.scss", "classes"],
    IntInput: ["game-ui/common/input/text/int-input.tsx", "IntInput"],
    BoundIntInputField: ["game-ui/game/widgets/field/int-input-field.tsx", "BoundIntInputField"],
    textInputTheme: ["game-ui/game/components/selected-info-panel/shared-components/text-input/text-input.module.scss", "classes"],
    mouseToolOptionsTheme: ["game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss", "classes"],
    ellipsesTextInputTheme: ["game-ui/common/input/text/ellipsis-text-input/ellipsis-text-input.module.scss", "classes"],
    EllipsisTextInput: ["game-ui/common/input/text/ellipsis-text-input/ellipsis-text-input.tsx", "EllipsisTextInput"],
    assetGridTheme: ["game-ui/game/components/asset-menu/asset-grid/asset-grid.module.scss", "classes"],

    FOCUS_DISABLED: ["game-ui/common/focus/focus-key.ts", "FOCUS_DISABLED"],
    FOCUS_AUTO: ["game-ui/common/focus/focus-key.ts", "FOCUS_AUTO"],
    useUniqueFocusKey: ["game-ui/common/focus/focus-key.ts", "useUniqueFocusKey"],
    useScrollController: ["game-ui/common/hooks/use-scroll-controller.tsx", "useScrollController"],
};

export class VanillaComponentResolver {
    // As far as I know you should not need to edit this portion here.
    // This was written by Klyte for his mod's UI but I didn't have to make any edits to it at all.
    public static get instance(): VanillaComponentResolver {
        return this._instance!!;
    }
    private static _instance?: VanillaComponentResolver;

    public static setRegistry(in_registry: ModuleRegistry) {
        this._instance = new VanillaComponentResolver(in_registry);
    }
    private registryData: ModuleRegistry;

    constructor(in_registry: ModuleRegistry) {
        this.registryData = in_registry;
    }

    private cachedData: Partial<Record<keyof typeof registryIndex, any>> = {};
    private updateCache(entry: keyof typeof registryIndex) {
        const entryData = registryIndex[entry];
        return (this.cachedData[entry] = this.registryData.registry.get(entryData[0])!![entryData[1]]);
    }

    // This section defines your components and themes in a way that you can access via the singleton in your components.
    // Replace the names, props, and strings as needed for your mod.
    public get Section(): (props: PropsSection) => JSX.Element {
        return this.cachedData["Section"] ?? this.updateCache("Section");
    }
    public get ToolButton(): (props: PropsToolButton) => JSX.Element {
        return this.cachedData["ToolButton"] ?? this.updateCache("ToolButton");
    }
    public get Toggle(): (props: ToggleProps) => JSX.Element {
        return this.cachedData["Toggle"] ?? this.updateCache("Toggle");
    }
    public get Checkbox(): (props: Checkbox) => JSX.Element {
        return this.cachedData["Checkbox"] ?? this.updateCache("Checkbox");
    }
    public get IntInput(): (props: IntInput) => JSX.Element {
        return this.cachedData["IntInput"] ?? this.updateCache("IntInput");
    }
    public get BoundIntInputField(): (props: BoundIntInputField) => JSX.Element {
        return this.cachedData["BoundIntInputField"] ?? this.updateCache("BoundIntInputField");
    }
    public get EllipsisTextInput(): (props: EllipsisTextInput) => JSX.Element {
        return this.cachedData["EllipsisTextInput"] ?? this.updateCache("EllipsisTextInput");
    }
    public get toggleTheme(): Theme | any {
        return this.cachedData["toggleTheme"] ?? this.updateCache("toggleTheme");
    }
    public get checkboxTheme(): Theme | any {
        return this.cachedData["checkboxTheme"] ?? this.updateCache("checkboxTheme");
    }
    public get mouseToolOptionsTheme(): Theme | any {
        return this.cachedData["mouseToolOptionsTheme"] ?? this.updateCache("mouseToolOptionsTheme");
    }
    public get toolButtonTheme(): Theme | any {
        return this.cachedData["toolButtonTheme"] ?? this.updateCache("toolButtonTheme");
    }
    public get textInputTheme(): TextInputTheme | Theme | any {
        return this.cachedData["textInputTheme"] ?? this.updateCache("textInputTheme");
    }
    public get ellipsesTextInputTheme(): EllipsesTextInputTheme | Theme | any {
        return this.cachedData["ellipsesTextInputTheme"] ?? this.updateCache("ellipsesTextInputTheme");
    }
    public get assetGridTheme(): Theme | any {
        return this.cachedData["assetGridTheme"] ?? this.updateCache("assetGridTheme");
    }

    public get FOCUS_DISABLED(): UniqueFocusKey {
        return this.cachedData["FOCUS_DISABLED"] ?? this.updateCache("FOCUS_DISABLED");
    }
    public get FOCUS_AUTO(): UniqueFocusKey {
        return this.cachedData["FOCUS_AUTO"] ?? this.updateCache("FOCUS_AUTO");
    }
    public get useUniqueFocusKey(): (focusKey: FocusKey, debugName: string) => UniqueFocusKey | null {
        return this.cachedData["useUniqueFocusKey"] ?? this.updateCache("useUniqueFocusKey");
    }
    public get useScrollController(): () => ScrollController {
        return this.cachedData["useScrollController"] ?? this.updateCache("useScrollController");
    }
}