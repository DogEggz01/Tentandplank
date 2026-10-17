# Tentandplank
*Disclaimer: made with opus 5.5*

This is a feature mod separate from Economic Overhaul, for people who don't want the economic change.
I will just copy the readme so i don't need to write twice.

Difference is that Water damage and impact damage is default off. So you can use tarp and pole just for aesthetic.

## Cargo water damage (CAN BE TOGGLE OFF IN CONFIGURATOR)
<img width="3840" height="2160" alt="207573~1" src="https://github.com/user-attachments/assets/90010956-ad69-4ded-a76f-fd009b3750a5" />

- Most of the cargo will now receive water damage when they are soaked in water, under rain, or drop into sea.
  - Trade goods that is not affect by water damage: 
    - coconuts, gems, iron, gold, copper, tools, sculptures, logs, nails, marble, silver, sulfur, rubber.
  - Water damage from soaking in water will start when cargo soaked 15% of it's height in water
    - This means make cargo stand on narror side will make it less vulnerable to water damage.
- Add water damage hint text for all water vulnerable trade good, when point at it will show current water damage level.
  - It start from 0%, progress to 100%.
  - This hint text can be toggle off in Configurator
- Cargo that receive water damage will receive price penalty. For mission cargo, it also affect reputation.
  - This effect start at 10% water damage, so you have some buffer.
  - Loan principal and interest won't get discount, pay up sucker
- Liquid cargo (All alcohol and water) also receive water damage, but won't get their price reduce until 100% water damage. When at 100% water damage, liquid cargo lose value.
  - You can still drink 100% water damage alcohol and water, you will be fine.
- When water level in ship pass 5% of the cargo height, it will count as soaked and start damaging the cargo.
  - This means smaller cargo is more easily to get damage, also this means you can avoid this by elevated the cargo.
  - Water level over 5% don't increase damage rate. Soaking damage rate is fixed.
- After 10% of water damage, your cargo will start having water stain, that's your visual cue that your cargo start decreasing value.
- Water damage will also deepen your cargo's crate color.
- Wetted cargo can be dried. As long as it's water damage is below 80% (include 80%). It can be dried back up to 20%
  - 20% water damage will result in 90% good price
- Trade good using luxury curve can only be dried up to 50% (60% of good price)
- Liquid cargo cannot be dried. So don't let it reach 100%.
- Drying requires you to place cargo under open sky. Under full open sky and clear weather, every in game day will dry 30% of water damage.
  - If the cargo is covered by something on top of it partially, it will still dry, but slower.
  - Fully covered cargo (either by other cargo, or by the ship structure) will not dry.
  - Cargo that have water touching it but below it's 5% height will also not dry.
- Rain will damage uncovered cargo. Partially covered cargo still get rain damage, but at slower speed.
  - If you are on small ship that don't have many hull structure to provide cover. Put things on top of cargo to cover it also work.
- The speed that cargo get damage is tied to rain intensity. Inside the storm and at the outskirt of storm will have diffent water damage rate.
- Water damage from rain and soaked water will stack.
- Water damage will only count when ship is near you, so you don't need to worry far away cargo on your other ship become wet
## Fragile Cargo
- Gem, Marble and Sculpture is consider fragile cargo.
- Fragile cargo will receive damage whenever ship receive impact damage. Normal wearing damage won't affect it.
- Each percent of damage reduce it's value (and rep if it's mission cargo) accordingly. So a 50% damaged cargo is at 50% value and rep reward.
- Damage cargo cannot be repair, and cargo that reach 0 health will lose all it's value, unlike water damaged cargo.
- Tools have chance to be spawn as fragile cargo in cargo mission.
### Wood plank for repairing
- Wood plank item is added so you have another way to counter the water damage.
- You can aquire wood plank in 2 way
  - Unbundle Lumber trade good, which gives you 128 plank.
  - Buy Wood Plank Bundle from Kicia bay furniture vendor. After unbundle it gives you 8 plank.
- To fix the hull, attach the wood plank at any part of the hull, and start hammering it.
- Each plank need 20s real life time to hammer. If the ship is already caulked with oakum, hammering time reduce by half.
  - A reminder that oakum lose 12% each day, remember to fill it up.
- Plank needed to fix 1% damage is related to shipyard repair fee. 
  - When aquire plank from lumber, it will roughtly be same cost as repairing in shipyard
  - When aquire plank from Wood Plank Bundle, it will be 1.6x the shipyard repair cost.
  - Currently most expansive hull - Jong, need 20 plank to fix 1% damage.
- Plank fixing fee is calculated with 1x lumber price, so if lumber become more expensive/cheaper, it will change your repairing fee.
- If you remove the plank, it's hammering progress will be reset
- If the lumber is wet, you will get less plank, depends on how wet it is.
### Canvas pack for rain shelter
<img width="1536" height="864" alt="image" src="https://github.com/user-attachments/assets/544e796a-4dce-4e3e-b087-f69c2185ec88" />

- Canvas pack now sold at every vendor that also sell oakum
- When holding the pack, you can aim at ship's fixed surface and line(shroud/stay), or any item you hammered on board, press R to set the corner of the tarp, you need to set 4 corner.
  - If you misclick, drop the pack will reset the set corner.
   - All 4 corner need to be either on land or on ship.
  - If you unnailed the item that is tied to tarp, it will drop and goes back to canvas pack.
- To take off the tarp, go to one of the corner and press R. You will get the canvas pack back.
- Tarp will act like full cover, protecting it from rain, but also prevent it from drying. Partial cover/drying work like before.
- Tarp have 10 in game days lifespan, it will become darker overtime, on 10th day, it will be torn and stop provide cover.
  - Taking out the torn tarp won't produce the canvas pack.
  - Tarp in Canvas pack won't progress their lifespan, you can keep it forever
- Tarp you left on other ship will still progress their lifespan, remember to take it down if you want to go sail other ship!
- You can repair tarp by using another canvas pack. Hold the canvas pack, aim at 1 of the corner and press R.
### Wood pole
<img width="3840" height="2160" alt="20E978~1" src="https://github.com/user-attachments/assets/76e1bdbb-07ce-415f-9337-56bec8d42296" />

- Sold beside canvas pack, as a 4 poles bundle
- Press Q/E to increase/decrease pole length. It can be attached like lantern hook onto surface.
- Pole will be attached at where you aim, scrolling mouse wheel will rotate at the attached point.
  - Holding Q while scrolling will rotate left/right instead.
- After nailed, it can be use to attach tarp (don't have to be attaching, just nailed will do)
- Wood pole can support tarp after nailed, so you can make tent/bimini like structure
- If unnailed the pole, the tarp will be drop or become unsupported.
- All wall attachtable item can attach to nailed wood pole, including other wood pole
- If nailed the item on wood pole, when you pick up the wood pole it will stay on the pole.
  - If you didn't nail the item, it will be drop when picked up
  - When there is item nailed on the pole, you cannot change it's length.
- Press H while holding a wood pole to switch to bracing mode, under this mod you can push the ship, fending and punting.
  - To push, just treat the tip of the pole as extension of your hand, still press F to push.
  - To fend, aim the pole at any surface, when it touch something it will automatically fend for you.
    - At 8m long (longest length for a single pole), you can stop a ship running at 6 knots toward the solid things.
  - To punt, aim your pole at sea bed and press F like you push the ship.
- Under bracing mode, pressing F won't drop the pole, pressing T (throw button) still drop it.
- If you nailed a pole on another pole, the extra length can be used for push/fend/punt also.
