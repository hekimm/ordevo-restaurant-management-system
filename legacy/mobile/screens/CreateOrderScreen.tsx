import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
  useWindowDimensions,
} from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { createMaterialTopTabNavigator } from '@react-navigation/material-top-tabs';
import { RootStackParamList } from '../types';
import { supabase, MenuItem, MenuCategory } from '../lib/supabase';

type Props = NativeStackScreenProps<RootStackParamList, 'CreateOrder'>;

interface CartItem {
  menuItem: MenuItem;
  quantity: number;
}

const Tab = createMaterialTopTabNavigator();

// Kategori bazlı ürün listesi komponenti
function CategoryProductList({
  categoryId,
  menuItems,
  cart,
  onAddToCart,
}: {
  categoryId: string | null;
  menuItems: MenuItem[];
  cart: CartItem[];
  onAddToCart: (item: MenuItem) => void;
}) {
  const filteredItems = categoryId
    ? menuItems.filter((item) => item.category_id === categoryId)
    : menuItems;

  if (filteredItems.length === 0) {
    return (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyText}>Bu kategoride ürün bulunmuyor</Text>
      </View>
    );
  }

  return (
    <FlatList
      data={filteredItems}
      renderItem={({ item }) => (
        <TouchableOpacity
          style={styles.menuItem}
          onPress={() => onAddToCart(item)}
          activeOpacity={0.7}
        >
          <View style={styles.menuItemInfo}>
            <Text style={styles.menuItemName}>{item.name}</Text>
            {item.description && (
              <Text style={styles.menuItemDesc} numberOfLines={2}>
                {item.description}
              </Text>
            )}
            <Text style={styles.menuItemPrice}>₺{item.price.toFixed(2)}</Text>
          </View>
          {cart.find((c) => c.menuItem.id === item.id) && (
            <View style={styles.quantityBadge}>
              <Text style={styles.quantityText}>
                {cart.find((c) => c.menuItem.id === item.id)?.quantity}
              </Text>
            </View>
          )}
        </TouchableOpacity>
      )}
      keyExtractor={(item) => item.id}
      contentContainerStyle={styles.listContainer}
      showsVerticalScrollIndicator={false}
    />
  );
}

export default function CreateOrderScreen({ route, navigation }: Props) {
  const { tableId } = route.params;
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [cart, setCart] = useState<CartItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const layout = useWindowDimensions();

  useEffect(() => {
    loadMenu();
  }, []);

  const loadMenu = async () => {
    try {
      const [categoriesRes, itemsRes] = await Promise.all([
        supabase.from('menu_categories').select('*').order('sort_order'),
        supabase
          .from('menu_items')
          .select('*, category:menu_categories(*)')
          .eq('is_active', true),
      ]);

      if (categoriesRes.data) setCategories(categoriesRes.data);
      if (itemsRes.data) setMenuItems(itemsRes.data);
    } catch (error: any) {
      Alert.alert('Hata', 'Menü yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const addToCart = (item: MenuItem) => {
    const existing = cart.find((c) => c.menuItem.id === item.id);
    if (existing) {
      setCart(
        cart.map((c) =>
          c.menuItem.id === item.id ? { ...c, quantity: c.quantity + 1 } : c
        )
      );
    } else {
      setCart([...cart, { menuItem: item, quantity: 1 }]);
    }
  };

  const removeFromCart = (itemId: string) => {
    const existing = cart.find((c) => c.menuItem.id === itemId);
    if (existing && existing.quantity > 1) {
      setCart(
        cart.map((c) =>
          c.menuItem.id === itemId ? { ...c, quantity: c.quantity - 1 } : c
        )
      );
    } else {
      setCart(cart.filter((c) => c.menuItem.id !== itemId));
    }
  };

  const createOrder = async () => {
    if (cart.length === 0) {
      Alert.alert('Uyarı', 'Lütfen en az bir ürün ekleyin');
      return;
    }

    setCreating(true);
    try {
      const { data: userData } = await supabase.auth.getUser();
      if (!userData.user) throw new Error('Kullanıcı bulunamadı');

      // Sabit organizasyon ID'sini kullan
      const { getOrganizationId } = await import('../config/organization');
      const organizationId = getOrganizationId();

      // Sipariş oluştur
      const { data: order, error: orderError } = await supabase
        .from('orders')
        .insert({
          organization_id: organizationId,
          table_id: tableId,
          status: 'open',
          opened_by_user_id: userData.user.id,
          opened_at: new Date().toISOString(),
        })
        .select()
        .single();

      if (orderError) throw orderError;

      // Sipariş ürünlerini ekle
      const orderItems = cart.map((item) => ({
        organization_id: organizationId,
        order_id: order.id,
        menu_item_id: item.menuItem.id,
        quantity: item.quantity,
        unit_price: item.menuItem.price,
        total_price: item.menuItem.price * item.quantity,
        status: 'pending',
        created_by_user_id: userData.user.id,
      }));

      const { error: itemsError } = await supabase
        .from('order_items')
        .insert(orderItems);

      if (itemsError) throw itemsError;

      Alert.alert('Başarılı', 'Sipariş oluşturuldu', [
        {
          text: 'Tamam',
          onPress: () => navigation.navigate('OrderDetail', { orderId: order.id }),
        },
      ]);
    } catch (error: any) {
      Alert.alert('Hata', error.message);
    } finally {
      setCreating(false);
    }
  };

  const totalAmount = cart.reduce(
    (sum, item) => sum + item.menuItem.price * item.quantity,
    0
  );

  if (loading) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Tab.Navigator
        initialLayout={{ width: layout.width }}
        screenOptions={{
          tabBarScrollEnabled: true,
          tabBarActiveTintColor: '#3B82F6',
          tabBarInactiveTintColor: '#6B7280',
          tabBarLabelStyle: {
            fontSize: 14,
            fontWeight: '600',
            textTransform: 'none',
          },
          tabBarStyle: {
            backgroundColor: '#fff',
            elevation: 0,
            shadowOpacity: 0,
            borderBottomWidth: 1,
            borderBottomColor: '#E5E7EB',
          },
          tabBarIndicatorStyle: {
            backgroundColor: '#3B82F6',
            height: 3,
          },
          swipeEnabled: true,
        }}
      >
        <Tab.Screen
          name="Tümü"
          children={() => (
            <CategoryProductList
              categoryId={null}
              menuItems={menuItems}
              cart={cart}
              onAddToCart={addToCart}
            />
          )}
        />
        {categories.map((category) => (
          <Tab.Screen
            key={category.id}
            name={category.name}
            children={() => (
              <CategoryProductList
                categoryId={category.id}
                menuItems={menuItems}
                cart={cart}
                onAddToCart={addToCart}
              />
            )}
          />
        ))}
      </Tab.Navigator>

      {cart.length > 0 && (
        <View style={styles.cartContainer}>
          <View style={styles.cartHeader}>
            <Text style={styles.cartTitle}>Sepet ({cart.length} ürün)</Text>
            <Text style={styles.cartTotal}>₺{totalAmount.toFixed(2)}</Text>
          </View>

          <FlatList
            data={cart}
            horizontal
            renderItem={({ item }) => (
              <View style={styles.cartItem}>
                <Text style={styles.cartItemName}>{item.menuItem.name}</Text>
                <View style={styles.cartItemActions}>
                  <TouchableOpacity
                    onPress={() => removeFromCart(item.menuItem.id)}
                    style={styles.cartButton}
                  >
                    <Text style={styles.cartButtonText}>-</Text>
                  </TouchableOpacity>
                  <Text style={styles.cartItemQuantity}>{item.quantity}</Text>
                  <TouchableOpacity
                    onPress={() => addToCart(item.menuItem)}
                    style={styles.cartButton}
                  >
                    <Text style={styles.cartButtonText}>+</Text>
                  </TouchableOpacity>
                </View>
              </View>
            )}
            keyExtractor={(item) => item.menuItem.id}
            showsHorizontalScrollIndicator={false}
          />

          <TouchableOpacity
            style={[styles.createButton, creating && styles.createButtonDisabled]}
            onPress={createOrder}
            disabled={creating}
          >
            {creating ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.createButtonText}>Sipariş Oluştur</Text>
            )}
          </TouchableOpacity>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F9FAFB',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 32,
    backgroundColor: '#F9FAFB',
  },
  emptyText: {
    fontSize: 16,
    color: '#6B7280',
    textAlign: 'center',
  },
  listContainer: {
    padding: 16,
    paddingBottom: 200,
    backgroundColor: '#F9FAFB',
  },
  menuItem: {
    backgroundColor: '#fff',
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  menuItemInfo: {
    flex: 1,
  },
  menuItemName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1F2937',
    marginBottom: 4,
  },
  menuItemDesc: {
    fontSize: 14,
    color: '#6B7280',
    marginBottom: 4,
  },
  menuItemPrice: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#3B82F6',
  },
  quantityBadge: {
    backgroundColor: '#3B82F6',
    width: 32,
    height: 32,
    borderRadius: 16,
    justifyContent: 'center',
    alignItems: 'center',
  },
  quantityText: {
    color: '#fff',
    fontWeight: 'bold',
  },
  cartContainer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    backgroundColor: '#fff',
    padding: 16,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: -2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 5,
  },
  cartHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  cartTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1F2937',
  },
  cartTotal: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#3B82F6',
  },
  cartItem: {
    backgroundColor: '#F3F4F6',
    padding: 12,
    borderRadius: 8,
    marginRight: 8,
    minWidth: 120,
  },
  cartItemName: {
    fontSize: 14,
    fontWeight: '500',
    marginBottom: 8,
  },
  cartItemActions: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  cartButton: {
    backgroundColor: '#3B82F6',
    width: 28,
    height: 28,
    borderRadius: 14,
    justifyContent: 'center',
    alignItems: 'center',
  },
  cartButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  cartItemQuantity: {
    fontSize: 16,
    fontWeight: 'bold',
  },
  createButton: {
    backgroundColor: '#10B981',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 12,
  },
  createButtonDisabled: {
    opacity: 0.6,
  },
  createButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});
