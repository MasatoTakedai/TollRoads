import React, { FC, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Panel, Scrollable, Portal, DraggablePanelProps, Number2 } from 'cs2/ui'; // Updated import
import styles from './TollRoadsMenu.module.scss';
import { trigger, useValue } from "cs2/api";
import { TollRoadItem } from "domain/TollRoadItem"
import { id } from "mod.json"
import { tollRoads$ } from '../bindings';
import { tollRoadsToolEnabled$ } from "mods/bindings";


// TollLane row prop
interface TollRoadProps extends DraggablePanelProps {
    tollRoad: TollRoadItem;
    showAll?: boolean;
}

const TollRoadRow: React.FC<TollRoadProps> = ({ tollRoad, showAll = true }) => {
    if (!showAll) return null;

    const handleNavigate = () => {
        trigger(id, "NavigateTo", tollRoad.Entity); // Call C# method
    };

    return (
        <div
            className="labels_L7Q row_S2v"
            style={{
                width: '100%',
                padding: '1rem 1rem',
                display: 'flex',
                alignItems: 'center',
                boxSizing: 'border-box',
            }}
        >
            <div style={{
                flex: '0 0 40%',
                paddingRight: '1rem',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap'
            }}>
                {tollRoad.Name}
            </div>
            <div style={{ flex: '0 0 18%', textAlign: 'center' }}>
                {tollRoad.Toll}
            </div>
            <div style={{ flex: '0 0 18%', textAlign: 'center' }}>
                {tollRoad.Revenue}
            </div>
            <div style={{ flex: '0 0 14%', textAlign: 'center' }}>
                <button
                    onClick={handleNavigate}
                    style={{
                        padding: '0.5rem 1rem',
                        backgroundColor: '#007bff',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        fontSize: '14px'
                    }}
                >
                    Navigate
                </button>
            </div>
        </div>
    );
};



// TollLane menu prop
interface TollRoadMenuProps extends DraggablePanelProps {

}

// Simple horizontal line
const DataDivider: React.FC = () => (
    <div style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}>
        <div style={{ borderBottom: '1px solid gray', width: '100%' }}></div>
    </div>
);

const TollRoadMenu: FC<TollRoadMenuProps> = ({ onClose, initialPosition, ...props }) => {
    // State for controlling the visibility of the panel
    const initialPos: Number2 = { x: 0.038, y: 0.15 };
    const tollRoadsToolEnabled = useValue(tollRoadsToolEnabled$)
    const tollRoads = useValue(tollRoads$)

    if (!tollRoadsToolEnabled) return null;
    return (
        <Panel

            draggable={true}
            initialPosition={initialPos}
            onClose={onClose}
            className={styles.panel}

            header={(
                <div className={styles.header}>
                    <span className={styles.headerText}>Toll Roads</span>
                </div>
            )}>

            {tollRoads.length === 0 ? (
                <p>No Toll Roads</p>
            ) : (
                <div>
                    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '0 25rem' }}>
                        <div
                            className="labels_L7Q row_S2v"
                            style={{
                                width: '100%',
                                padding: '1rem 0',
                                display: 'flex',
                                alignItems: 'center',
                            }}
                        >
                            <div style={{ flex: '0 0 40%' }}>
                                <div><b>Road Name</b></div>
                            </div>
                            <div style={{ flex: '0 0 23%', textAlign: 'center' }}>
                                <b>Toll</b>
                            </div>
                            <div style={{ flex: '0 0 23%', textAlign: 'center' }}>
                                <b>Revenue</b>
                            </div>
                            <div style={{ flex: '0 0 14%', textAlign: 'center' }}>
                                <b></b>
                            </div>
                        </div>
                    </div>

                    <DataDivider />

                    {/* Road List */}
                    <div style={{ padding: '1rem 0' }}>
                        <Scrollable smooth={true} vertical={true} trackVisibility="scrollable">
                            {tollRoads.map((road) => (
                                <TollRoadRow
                                    key={road.Entity}
                                    tollRoad={road}
                                />
                            ))}
                        </Scrollable>
                    </div>

                    <DataDivider />
                </div>
            )}
        </Panel>
    );
};

export default TollRoadMenu;