
item_to_break = 2735
hit_required_to_break = 4




anti_trash_ids = {2735,3364,3365,3366}



standX,standY = 46,29
standPosition = Vector2i.new(standX,standY)


maximum_block = math.floor(300 / hit_required_to_break)


client = getClient()
global_farm_world = ""
receivedFarmPacket = false


NON_BREAKABLE_BLOCKS = {
   110,2212,37
}

InventoryItemType = {
    block = 0,
    background = 1,
    seed = 2,
    water = 3,
    wearable = 4,
    weapon = 5,
    throwable = 6,
    consumable = 7,
    shard = 8,
    blueprint = 9,
    familiar = 10,
    food = 11,
    wiring = 12
}

local TypeMap = {
    [0] = "block", [1] = "background", [2] = "seed", [3] = "water",
    [4] = "wearable", [5] = "weapon", [6] = "throwable", [7] = "consumable",
    [8] = "shard", [9] = "blueprint", [10] = "familiar", [11] = "food", [12] = "wiring"
}

function auto_trash()
    for _, item in pairs(getInventory().items) do
        local should_trash = true

        for _, protected_id in pairs(anti_trash_ids) do
            if item.id == protected_id then
                should_trash = false
                break
            end
        end

        if should_trash then
            local type_key = TypeMap[item.type]
            if type_key then
                local itemTypeEnum = InventoryItemType[type_key] 

                if get_item_count(item.id, itemTypeEnum) > 0 then
                    client:trash(item.id, itemTypeEnum, get_item_count(item.id,itemTypeEnum))
                    sleep(1000)
                end
            end
        end
    end
end

function harvestable(tile)
    if not tile or tile.foreground == 0 then return false end
    local tree = tile:tree()
    return tree ~= nil and tree:ready()
end

function has_harvestable_trees()
    local world = getWorld()
    if not world then return false end
    for y = 59, 0, -1 do
        for x = 0, 79 do
            if harvestable(world:tile(Vector2i.new(x, y))) then
                return true
            end
        end
    end
    return false
end

function auto_harvest()
    local iterations = 0
    while true do
        iterations = iterations + 1
        ensure_is_in_world(global_farm_world)
        if get_item_count(item_to_break) > 998 then 
            break 
        end
        
        if iterations % 30 == 0 then auto_trash() end
        
        local world = client:world()
        if not world then break end
        
        local target_pos = nil
        for y = 59, 0, -1 do
            for x = 0, 79 do
                local tile = world:tile(Vector2i.new(x, y))
                if harvestable(tile) then
                    target_pos = Vector2i.new(x, y)
                    break
                end
            end
            if target_pos then break end
        end
        
        if target_pos then
            local my_pos = client:point()
            local dist = math.abs(my_pos.x - target_pos.x) + math.abs(my_pos.y - target_pos.y)
            
            if dist > 2 then
                client:findPath(target_pos)
                while client:pathfinding() do
                    sleep(50)
                end
            end
            
            my_pos = client:point()
            if math.abs(my_pos.x - target_pos.x) <= 2 and math.abs(my_pos.y - target_pos.y) <= 2 then
                local current_world = getWorld()
                if current_world and harvestable(current_world:tile(target_pos)) then
                    client:hit(target_pos)
                    sleep(100)
                end
            end
            collect_nearby_items(target_pos, 5)

        else
            break
        end
    end
end

function plant_seed(seed_id)
    ensure_is_in_world(global_farm_world)
    local amount = client:inventory():count(seed_id, InventoryItemType.seed)
    if amount <= 0 then 
        return 
    end
    
    local world = getWorld()
    if not world then return end
    
    local iterations = 0
    for y = 29, 3, -1 do 
        for x = 1, 78 do
            iterations = iterations + 1
            ensure_is_in_world(global_farm_world)
            if iterations % 30 == 0 then auto_trash() end
            
            if client:inventory():count(seed_id, InventoryItemType.seed) <= 0 then 
                return 
            end
            
            local tile = getWorld():tile(Vector2i.new(x, y))
            local below = getWorld():tile(Vector2i.new(x, y+1))
            
            if tile.foreground == 0 and below.foreground ~= 0 and not is_non_breakable(below.foreground) then
                local my_pos = client:point()
                local dist = math.abs(my_pos.x - x) + math.abs(my_pos.y - y)
                
                if dist > 2 then
                    client:findPath(Vector2i.new(x, y))
                    while client:pathfinding() do
                        sleep(50)
                    end
                end
                
                client:place(Vector2i.new(x, y), seed_id, InventoryItemType.seed)
                sleep(100)
            end
        end
    end
end


function is_online()
    if client:connected() and client:ping() > 0 then
        return true
    else
        return false
    end
end

function ensure_is_in_world(world_name)
    if not is_online() or get_nav() ~= world_name:lower() then
        WarpTo(world_name)
    end
end

function is_in_tile(x, y)
    local myPoint = client:point()
    local result = myPoint.x == x and myPoint.y == y
    return result
end

function wait_for_tutorial_to_finish()
    if client:isInTutorial() == false then
        return true
    end
    while client:isInTutorial() do
        sleep(1000)
        if client:isInTutorial() == false then
            break
        end
    end
    return false
end



function breakBlocks(targetBlock, maxBlocks, minHits)
    client:on("presend", function(message)
        local amount = client:inventory():count(targetBlock, InventoryItemType.block)
        local clientPoint = client:point()

        local positions = {
            Vector2i.new(clientPoint.x - 2, clientPoint.y + 1),
            Vector2i.new(clientPoint.x - 1, clientPoint.y + 1),
            Vector2i.new(clientPoint.x + 0, clientPoint.y + 1),
            Vector2i.new(clientPoint.x + 1, clientPoint.y + 1),
            Vector2i.new(clientPoint.x + 2, clientPoint.y + 1)
        }

        local totalBlocks = math.min(amount, maxBlocks)
        local posIndex = 1

        for i = 1, totalBlocks do
            local tilePoint = positions[posIndex]

            client:place(tilePoint, targetBlock, InventoryItemType.block)

            for j = 1, minHits do
                client:send("HB", {
                    x = tilePoint.x,
                    y = tilePoint.y
                })
            end

            posIndex = posIndex + 1
            if posIndex > #positions then
                posIndex = 1
            end
        end
    end)
end

function collect_nearby_items(center_point, radius)
    radius = radius or 5
    for i = 1, 5 do
        local world = client:world()
        if not world then return end
        
        local found_in_this_pass = false
        for id, collectable in pairs(world.collectables) do
            local item_pos = collectable:point()
            local dist = math.abs(center_point.x - item_pos.x) + math.abs(center_point.y - item_pos.y)
            if dist <= radius then
                client:collect(id)
                sleep(10)
                found_in_this_pass = true
            end
        end
        
        if not found_in_this_pass then
            if i > 1 then break end 
        end
        sleep(1)
    end
end

function collect_all_items()
    while true do
        ensure_is_in_world(global_farm_world)
        local world = client:world()
        
        if not world then
            sleep(1000)
        else
            local items = world.collectables
            local nearest_id = nil
            local min_dist = 9999
            local my_pos = client:point()
            
            local found_any = false
            for id, collectable in pairs(items) do
                found_any = true
                local item_pos = collectable:point()
                local dist = math.abs(my_pos.x - item_pos.x) + math.abs(my_pos.y - item_pos.y)
                if dist < min_dist then
                    min_dist = dist
                    nearest_id = id
                end
            end
            
            if nearest_id then
                local item = items[nearest_id]
                if item then
                    local item_point = item:point()
                    client:findPath(item_point)
                    while client:pathfinding() do
                        sleep(1)
                        if not is_online() then break end
                    end
                    
                    if is_online() and get_nav() == global_farm_world:lower() then
                        client:collect(nearest_id)
                        sleep(10)
                    end
                end
            else
                break 
            end
            
            if not found_any then break end
        end
    end
end


function is_non_breakable(id)
    for _, block_id in ipairs(NON_BREAKABLE_BLOCKS) do
        if id == block_id then
            return true
        end
    end
    return false
end

function force_break(tx, ty, sx, sy)
    while true do
        ensure_is_in_world(global_farm_world)
        local world = getWorld()
        
        if world then
            local tile = world:tile(Vector2i.new(tx, ty))
            
            if not tile or tile.foreground == 0 or is_non_breakable(tile.foreground) then
                break
            end

            if not is_in_tile(sx, sy) then
                client:findPath(Vector2i.new(sx, sy))
                while client:pathfinding() do
                   sleep(1)
                end
            end

            client:hit(Vector2i.new(tx, ty))
            sleep(180)
        else
            sleep(1)
        end
    end
end

function clear_world()
    if global_farm_world == "" then
        get_global_farm_world()
        print("Global Farm World: " .. global_farm_world)
    end
    ensure_is_in_world(global_farm_world)
    local world = getWorld()
    if not world then return end

    local MAX_X = 79
    local MIN_X = 0
    local START_Y = 59
    local END_Y = 3

    for y = START_Y, END_Y, -1 do
        ensure_is_in_world(global_farm_world)
        force_break(MAX_X, y, MAX_X, math.min(y + 1, START_Y))
    end
    for y = START_Y, END_Y, -1 do
        ensure_is_in_world(global_farm_world)
        force_break(MIN_X, y, MIN_X, math.min(y + 1, START_Y))
    end

    for y = START_Y, END_Y, -1 do
        if y >= 30 or (y < 30 and y % 2 ~= 0) then
            ensure_is_in_world(global_farm_world)

            if y % 2 == 0 then
                for x = 1, MAX_X - 1 do
                    force_break(x, y, x - 1, y)
                end
            else
                for x = MAX_X - 1, 1, -1 do
                    force_break(x, y, x + 1, y)
                end
            end
        end
    end

    auto_trash()

end

function get_nav()
    return client:navigation():lower()
end

function WarpTo(world_name)
    while not is_online() do
        sleep(1000)
    end
    while is_online() and get_nav() ~= world_name:lower() do
        if get_nav() ~= world_name:lower() then
            client:warp(world_name:upper())
            for i = 1,10 do
                sleep(1000)
                if get_nav() == world_name:lower() then
                    break
                end
            end
        end
    end
end 


function get_item_count(item_id, item_type)
    return client:inventory():count(item_id, item_type or InventoryItemType.block)
end

function get_global_farm_world()
    if global_farm_world ~= "" then
        return global_farm_world
    end

    while not is_online() do
        sleep(1000)
    end

    receivedFarmPacket = false
    client:send("GWLW",{Idx = 0})

    local timeout = 0
    while is_online() and not receivedFarmPacket and timeout < 10000 do
        sleep(100)
        timeout = timeout + 100
    end

    receivedFarmPacket = false
    return global_farm_world
end



function check_is_locked(optional_world_name)
    local target_world = optional_world_name or global_farm_world
    
    if target_world == "" or get_nav() == target_world:lower() then
        local pos = Vector2i.new(client:point().x, client:point().y + 1)
        if getWorld():tile(pos).foreground == 2212 then
            return true
        end
    end
    return false
end

function create_my_first_world()
    local farm_world = get_global_farm_world()

    while is_online() and farm_world == "" do
        local random_name = "WRD" .. tostring(math.random(100000, 99999999))

        WarpTo(random_name)

        if get_nav() == random_name:lower() then
            if get_item_count(2212, InventoryItemType.block) >= 1 then

                local pos = Vector2i.new(client:point().x, client:point().y + 1)

                client:place(pos, 2212, InventoryItemType.block)

                sleep(2000)

                if check_is_locked(random_name) then
                    print("World created successfully")
                    farm_world = random_name:lower()
                    global_farm_world = farm_world
                end
            end
        end
    end
end


client:on("p:GWLW", function(msg)
    receivedFarmPacket = true
    if msg.W0 then
        global_farm_world = msg.W0.WorldName or ""
        print("Found farm world: " .. global_farm_world)
    else
        global_farm_world = ""
    end
end)

function ban_active_players()
    local whitelisted_id = "69cc1d7a236154b3b81674ca"
    if get_nav() == global_farm_world:lower() then
        for _, player in pairs (getWorld().players) do
            if player.id ~= whitelisted_id then
                client:send("BPl",{U = player.id})
            end
        end
    end
end

client:on("p:AnP", function(msg)
    local whitelisted_id = "69cc1d7a236154b3b81674ca"
    if get_nav() == global_farm_world:lower() then
        if msg.U ~= whitelisted_id then
            client:send("BPl",{U = msg.U})
        end
    end
end)  

function init_client()
    while not is_online() do
        sleep(1000)
    end
    while is_online() do
        print("wait_tutorial_to_finish")
        wait_for_tutorial_to_finish()
        if wait_for_tutorial_to_finish() and is_online() then
            local fw = get_global_farm_world()
            if fw == "" then
                print("No farm world found, creating one...")
                create_my_first_world()
            end
            if get_global_farm_world() ~= "" then
                WarpTo(global_farm_world)
            end
            if get_nav() == global_farm_world:lower() then
                clear_world()
                return true
	        end

        end
    end
end




function pnb()
    ensure_is_in_world(global_farm_world)
    if get_nav() == global_farm_world:lower() then
        if not is_in_tile(standX,standY) then
            client:findPath(standPosition)
            while client:pathfinding() do
                sleep(1)
            end
        end
        if is_in_tile(standX,standY) and get_item_count(item_to_break) > 0 then
            breakBlocks(item_to_break,maximum_block,hit_required_to_break)
            sleep(1800)
            client:disconnect()
            while not is_online() do sleep(100) end
        end
    end
end

function main()
    init_client()
    while true do
        ban_active_players()


        while get_item_count(item_to_break, InventoryItemType.block) > 0 do
            pnb()
			if get_item_count(item_to_break, InventoryItemType.block) >= 999 then

			else
				collect_all_items()
			end
            ban_active_players()
        end
        

        collect_all_items()
        auto_trash()


        while get_item_count(item_to_break, InventoryItemType.seed) > 0 or has_harvestable_trees() do
            ban_active_players()
            if get_item_count(item_to_break, InventoryItemType.seed) > 0 then
                plant_seed(item_to_break)
            end
            
            if has_harvestable_trees() then
                auto_harvest()
            end
            
            auto_trash()
            
            if get_item_count(item_to_break, InventoryItemType.seed) == 0 and not has_harvestable_trees() then
                break
            end
            sleep(1)
        end
        
        sleep(100)
    end
end

main()