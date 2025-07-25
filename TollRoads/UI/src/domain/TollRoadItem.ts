export interface TollRoadItem {
    Entity: Entity;
    Toll: number;
    Revenue: number;
    Volume: number;
    Name: string;
}

export interface Entity {
    index: number;
    version: number;
}