const fonts = {
  heading: { family: "Oswald", style: "SemiBold" },
  body: { family: "Inter", style: "Regular" },
  medium: { family: "Inter", style: "Medium" }
};
for (const font of Object.values(fonts)) await figma.loadFontAsync(font);

const page = figma.currentPage;
const C = {
  paper: "#E8D8B8", paperLight: "#F3E8CF", ink: "#292721", muted: "#655947",
  dark: "#181D1A", dark2: "#252B24", sage: "#637645", green: "#729447",
  amber: "#C78A2A", teal: "#2F7979", rust: "#9C512D", line: "#726043",
  danger: "#A7C92F"
};
const rgb = hex => { const n = parseInt(hex.slice(1), 16); return { r: ((n>>16)&255)/255, g: ((n>>8)&255)/255, b: (n&255)/255 }; };
const solid = (hex, opacity=1) => ({ type: "SOLID", color: rgb(hex), opacity });
function box(node,name,x,y,w,h){node.name=name;node.x=x;node.y=y;node.resize(w,h);return node;}
function frame(parent,name,x,y,w,h,fill=null,stroke=null,radius=0){const n=box(figma.createFrame(),name,x,y,w,h);n.fills=fill?[solid(fill)]:[];n.strokes=stroke?[solid(stroke,.75)]:[];n.strokeWeight=stroke?1:0;n.cornerRadius=radius;parent.appendChild(n);return n;}
function rect(parent,name,x,y,w,h,fill,radius=0,opacity=1){const n=box(figma.createRectangle(),name,x,y,w,h);n.fills=[solid(fill,opacity)];n.cornerRadius=radius;parent.appendChild(n);return n;}
function text(parent,name,value,x,y,w,h,size=16,color=C.ink,font="body",align="LEFT"){const n=box(figma.createText(),name,x,y,w,h);n.fontName=fonts[font];n.fontSize=size;n.fills=[solid(color)];n.textAlignHorizontal=align;n.textAlignVertical="CENTER";n.textAutoResize="NONE";n.characters=String(value);parent.appendChild(n);return n;}
function button(parent,name,label,x,y,w,h,active=true){const n=frame(parent,`figunity:button Button ${name}`,x,y,w,h,active?C.sage:C.dark2,C.amber,5);text(n,"Label",label,8,0,w-16,h,18,C.paperLight,"heading","CENTER");return n;}
function pill(parent,name,label,value,x,y,w,color=C.sage){const n=frame(parent,name,x,y,w,48,C.paperLight,C.line,4);rect(n,"Icon",10,10,28,28,color,14,.95);text(n,"Label",label,46,2,w-104,22,13,C.muted,"medium");text(n,"Value",value,w-70,0,58,48,18,color,"heading","RIGHT");return n;}
function dangerBar(parent,x,y,w,value){const n=frame(parent,"figunity:passive-slider Danger",x,y,w,12,null,null,6);rect(n,"Track",0,0,w,12,C.line,6,.3);rect(n,"Fill",0,0,w*value,12,C.danger,6);return n;}

let root = page.findOne(n => n.type === "FRAME" && n.name === "11 World Tile Tooltip Elementwise");
if (!root) { let maxX=0; for(const child of page.children) maxX=Math.max(maxX,child.x+child.width); root=figma.createFrame(); root.name="11 World Tile Tooltip Elementwise"; root.x=maxX+240; root.y=0; page.appendChild(root); }
else for(const child of [...root.children]) child.remove();
root.resize(1920,1080); root.fills=[solid("#556044",.18)]; root.clipsContent=true;

// Dimmed world-map reference field.
rect(root,"Map Dimmer",0,0,1920,1080,C.dark,0,.55);
for(let row=0;row<6;row++) for(let col=0;col<10;col++) {
  const tile=frame(root,`Map Tile ${row}-${col}`,90+col*174,26+row*174,160,160,row%2?"#8B7C49":"#777849",C.paperLight,5);
  tile.opacity=.34;
}

const tip=frame(root,"World Tile Tooltip",1120,110,700,800,C.paper,C.amber,8);
rect(tip,"Header Band",0,0,700,92,C.dark,8);
text(tip,"Type","NORMAL LOCATION",24,10,250,28,14,C.danger,"medium");
text(tip,"Title","WHEAT FIELD",24,36,440,46,31,C.paperLight,"heading");
text(tip,"Biome","GRASSLAND",520,18,150,28,14,C.amber,"medium","RIGHT");
text(tip,"Strength Label","THREAT 1",520,48,150,26,16,C.paperLight,"heading","RIGHT");

text(tip,"Risk Title","AREA STRENGTH",24,108,180,30,17,C.ink,"heading");
dangerBar(tip,210,117,326,.12);
text(tip,"Risk Value","12 / 100",552,106,120,34,15,C.sage,"medium","RIGHT");

text(tip,"Travel Title","TRAVEL EFFECTS / EACH SURVIVOR",24,156,360,32,17,C.ink,"heading");
pill(tip,"Travel Water","WATER","-1",24,194,204,C.teal);
pill(tip,"Travel Food","FOOD","-1",240,194,204,C.amber);
pill(tip,"Encounter Chance","ENCOUNTER","5%",456,194,216,C.rust);

text(tip,"Loot Title","POSSIBLE RESOURCES",24,266,300,32,17,C.ink,"heading");
pill(tip,"Loot Food","FOOD","4–9",24,304,204,C.green);
pill(tip,"Loot Scrap","SCRAP","1–3",240,304,204,C.amber);
pill(tip,"Loot Water","WATER","0–2",456,304,216,C.teal);
text(tip,"Rare Gear","RARE: COMMON EQUIPMENT  •  2%",24,362,648,30,14,C.muted,"medium");

text(tip,"Enemies Title","POTENTIAL ENEMIES",24,410,300,32,17,C.ink,"heading");
const enemies=[["ASH ROACH","WEAK","#637645"],["MIRE LEECH","WEAK","#637645"],["RADIATED HOUND","RARE","#9C512D"]];
enemies.forEach((e,i)=>{const y=450+i*52;const row=frame(tip,`Enemy ${e[0]}`,24,y,648,42,C.paperLight,C.line,4);rect(row,"Threat Dot",10,9,24,24,e[2],12);text(row,"Name",e[0],48,0,360,42,15,C.ink,"medium");text(row,"Tier",e[1],480,0,148,42,14,e[2],"heading","RIGHT");});

const route=frame(tip,"Route Summary",24,622,648,76,C.dark2,C.line,5);
text(route,"Route Label","ROUTE",16,4,90,28,14,C.amber,"heading");
text(route,"Route Value","3 OPEN CELLS  •  OUT + RETURN",112,4,510,28,15,C.paperLight,"medium");
text(route,"Route Cost","TOTAL PER SURVIVOR:  FOOD -3  /  WATER -4",16,36,606,28,14,C.paperLight,"medium");
button(tip,"GatherResources","GATHER RESOURCES",24,718,648,58,true);

// Roster panel included in the same imported prefab, hidden by runtime until gather/expedition action.
const roster=frame(root,"Squad Roster Modal",120,118,900,782,C.paper,C.amber,8);
rect(roster,"Roster Header",0,0,900,86,C.dark,8);
text(roster,"Roster Title","SELECT SCAVENGING TEAM",24,18,500,48,30,C.paperLight,"heading");
text(roster,"Roster Count","0 / 4",728,18,146,48,23,C.amber,"heading","RIGHT");
text(roster,"Mission","WHEAT FIELD  •  THREAT 1",24,100,500,36,19,C.ink,"heading");
text(roster,"Mission Costs","OUT + RETURN:  FOOD -3  /  WATER -4 EACH",456,100,418,36,14,C.muted,"medium","RIGHT");
const survivorNames=[["ASHA","SCAVENGING 62  •  POWER 18"],["BRAM","SURVIVAL 55  •  POWER 21"],["CORA","MEDICINE 72  •  POWER 14"],["DIMA","MECHANICS 64  •  POWER 17"],["ELKA","WOUNDED  •  UNAVAILABLE"],["FARO","SURVIVAL 60  •  POWER 16"]];
survivorNames.forEach((s,i)=>{const col=i%2,row=Math.floor(i/2),x=24+col*426,y=154+row*142;const card=frame(roster,`figunity:button Survivor ${s[0]}`,x,y,402,124,i===4?C.dark2:C.paperLight,i===0?C.amber:C.line,5);rect(card,"Portrait",10,10,94,104,i===4?C.muted:C.sage,4,.75);text(card,"Name",s[0],122,12,250,36,23,C.ink,"heading");text(card,"Stats",s[1],122,52,250,50,13,i===4?C.rust:C.muted,"medium");});
const totals=frame(roster,"Squad Totals",24,596,852,74,C.dark2,C.line,5);
text(totals,"Power","SQUAD POWER  0",18,0,244,74,17,C.paperLight,"heading");
text(totals,"Supplies","FOOD 0  •  WATER 0",278,0,282,74,15,C.paperLight,"medium","CENTER");
text(totals,"Risk","ESTIMATED RISK  —",584,0,244,74,16,C.amber,"heading","RIGHT");
button(roster,"Cancel","CANCEL",24,696,260,58,false);
button(roster,"SendTeam","SEND TEAM",300,696,576,58,true);

figma.currentPage.selection=[root];
figma.viewport.scrollAndZoomIntoView([root]);
return { frameId: root.id, frameName: root.name, tooltipChildren: tip.children.length, rosterChildren: roster.children.length };
