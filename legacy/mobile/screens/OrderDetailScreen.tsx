import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
  RefreshControl,
  Dimensions,
  Modal,
} from 'react-native';
import { BlurView } from 'expo-blur';
import { LinearGradient } from 'expo-linear-gradient';
import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '../types';
import { supabase, Order, OrderItem, MenuItem } from '../lib/supabase';

const { width, height } = Dimensions.get('window');

type Props = NativeStackScreenProps<RootStackParamList, 'OrderDetail'>;

export default function OrderDetailScreen({ route, navigation }: Props) {
  const { orderId } = route.params;
  const [order, setOrder] = useState<Order | null>(null);
  const [orderItems, setOrderItems] = useState<OrderItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showAddItemModal, setShowAddItemModal] = useState(false);
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [loadingMenu, setLoadingMenu] = useState(false);
  const [addingItem, setAddingItem] = useState(false);

  const loadOrderDetails = async () => {
    try {
      const [orderRes, itemsRes] = await Promise.all([
        supabase
          .from('orders')
          .select('*, table:restaurant_tables(*)')
          .eq('id', orderId)
          .single(),
        supabase
          .from('order_items')
          .select('*, menu_item:menu_items(*)')
          .eq('order_id', orderId),
      ]);

      if (orderRes.data) setOrder(orderRes.data);
      if (itemsRes.data) setOrderItems(itemsRes.data);
    } catch (error: any) {
      Alert.alert('Hata', 'Sipariş detayları yüklenemedi');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadOrderDetails();

    // Realtime subscription - order_items ve orders değişikliklerini dinle
    const channel = supabase
      .channel(`order-${orderId}-realtime`)
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'order_items',
        },
        (payload) => {
          console.log('Order items değişikliği:', payload);
          loadOrderDetails();
        }
      )
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'orders',
        },
        (payload) => {
          console.log('Order değişikliği:', payload);
          loadOrderDetails();
        }
      )
      .subscribe((status) => {
        console.log('Realtime subscription durumu:', status);
      });

    return () => {
      channel.unsubscribe();
    };
  }, [orderId]);

  const onRefresh = () => {
    setRefreshing(true);
    loadOrderDetails();
  };

  const deleteOrder = async () => {
    Alert.alert(
      'Siparişi Sil',
      'Bu siparişi tamamen silmek istediğinize emin misiniz? Bu işlem geri alınamaz!',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Sil',
          style: 'destructive',
          onPress: async () => {
            try {
              // 1. Sipariş kalemlerini sil
              const { error: itemsError } = await supabase
                .from('order_items')
                .delete()
                .eq('order_id', orderId);

              if (itemsError) throw itemsError;

              // 2. Ödemeleri sil (varsa)
              const { error: paymentsError } = await supabase
                .from('payments')
                .delete()
                .eq('order_id', orderId);

              if (paymentsError) throw paymentsError;

              // 3. Siparişi sil
              const { error: orderError } = await supabase
                .from('orders')
                .delete()
                .eq('id', orderId);

              if (orderError) throw orderError;

              Alert.alert('Başarılı', 'Sipariş silindi', [
                { text: 'Tamam', onPress: () => navigation.goBack() }
              ]);
            } catch (error: any) {
              Alert.alert('Hata', error.message);
            }
          },
        },
      ]
    );
  };

  const updateItemStatus = async (itemId: string, newStatus: string) => {
    try {
      const { error } = await supabase
        .from('order_items')
        .update({ status: newStatus })
        .eq('id', itemId);

      if (error) throw error;

      setOrderItems(
        orderItems.map((item) =>
          item.id === itemId ? { ...item, status: newStatus as any } : item
        )
      );

      Alert.alert('Başarılı', 'Durum güncellendi');
    } catch (error: any) {
      Alert.alert('Hata', error.message);
    }
  };

  const totalAmount = orderItems.reduce((sum, item) => sum + item.total_price, 0);

  const loadMenuItems = async () => {
    setLoadingMenu(true);
    try {
      const { data, error } = await supabase
        .from('menu_items')
        .select('*')
        .eq('is_active', true)
        .order('name');

      if (error) throw error;
      if (data) setMenuItems(data);
    } catch (error: any) {
      Alert.alert('Hata', 'Menü yüklenemedi');
    } finally {
      setLoadingMenu(false);
    }
  };

  const addItemToOrder = async (menuItem: MenuItem) => {
    setAddingItem(true);
    try {
      const { data: userData } = await supabase.auth.getUser();
      if (!userData.user) throw new Error('Kullanıcı bulunamadı');

      // Sabit organizasyon ID'sini kullan
      const { getOrganizationId } = await import('../config/organization');
      const organizationId = getOrganizationId();

      const { error } = await supabase
        .from('order_items')
        .insert({
          organization_id: organizationId,
          order_id: orderId,
          menu_item_id: menuItem.id,
          quantity: 1,
          unit_price: menuItem.price,
          total_price: menuItem.price,
          status: 'pending',
          created_by_user_id: userData.user.id,
        });

      if (error) throw error;

      Alert.alert('Başarılı', 'Ürün eklendi');
      setShowAddItemModal(false);
      loadOrderDetails();
    } catch (error: any) {
      Alert.alert('Hata', error.message);
    } finally {
      setAddingItem(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'pending': return { bg: 'rgba(251, 191, 36, 0.15)', text: '#F59E0B', iconName: 'time-outline' as const };
      case 'in_kitchen': return { bg: 'rgba(59, 130, 246, 0.15)', text: '#3B82F6', iconName: 'restaurant-outline' as const };
      case 'served': return { bg: 'rgba(34, 197, 94, 0.15)', text: '#22C55E', iconName: 'checkmark-circle-outline' as const };
      default: return { bg: 'rgba(156, 163, 175, 0.15)', text: '#6B7280', iconName: 'help-circle-outline' as const };
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'pending': return 'Bekliyor';
      case 'in_kitchen': return 'Hazırlanıyor';
      case 'served': return 'Servis Edildi';
      default: return 'Bilinmiyor';
    }
  };

  if (loading) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color="#D4AF37" />
        <Text style={styles.loadingText}>Sipariş yükleniyor...</Text>
      </View>
    );
  }

  if (!order) {
    return (
      <View style={styles.centerContainer}>
        <BlurView intensity={30} tint="light" style={styles.emptyBlur}>
          <Text style={styles.emptyText}>Sipariş bulunamadı</Text>
        </BlurView>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Animated Gradient Background */}
      <LinearGradient
        colors={['#FAFAFA', '#F5F5F5', '#FFFFFF', '#F8F8F8', '#FAFAFA']}
        locations={[0, 0.25, 0.5, 0.75, 1]}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 1 }}
        style={StyleSheet.absoluteFill}
      />

      {/* Light Streaks */}
      <View style={styles.lightStreak1}>
        <LinearGradient
          colors={['rgba(240, 240, 245, 0.5)', 'transparent']}
          start={{ x: 0, y: 0 }}
          end={{ x: 1, y: 1 }}
          style={styles.streakGradient}
        />
      </View>

      <View style={styles.lightStreak2}>
        <LinearGradient
          colors={['rgba(245, 245, 250, 0.4)', 'transparent']}
          start={{ x: 1, y: 0 }}
          end={{ x: 0, y: 1 }}
          style={styles.streakGradient}
        />
      </View>

      {/* Header Card */}
      <BlurView intensity={35} tint="light" style={styles.headerBlur}>
        <LinearGradient
          colors={['rgba(255, 255, 255, 0.3)', 'rgba(255, 255, 255, 0.1)']}
          style={styles.headerGradient}
        >
          <View style={styles.headerContent}>
            <View>
              <Text style={styles.tableName}>{order.table?.name}</Text>
              <Text style={styles.orderTime}>
                {new Date(order.opened_at).toLocaleTimeString('tr-TR', {
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </Text>
            </View>
            <View style={styles.headerActions}>
              <TouchableOpacity
                style={styles.addButton}
                onPress={() => {
                  setShowAddItemModal(true);
                  loadMenuItems();
                }}
                activeOpacity={0.7}
              >
                <Ionicons name="add-circle" size={24} color="#3B82F6" />
                <Text style={styles.addButtonText}>Ürün Ekle</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.deleteButton}
                onPress={deleteOrder}
                activeOpacity={0.7}
              >
                <Ionicons name="trash" size={20} color="#EF4444" />
              </TouchableOpacity>
              
              <View style={[
                styles.statusBadge,
                { backgroundColor: order.status === 'open' ? 'rgba(34, 197, 94, 0.15)' : 'rgba(156, 163, 175, 0.15)' }
              ]}>
                <Ionicons 
                  name={order.status === 'open' ? 'checkmark-circle' : 'close-circle'} 
                  size={16} 
                  color={order.status === 'open' ? '#22C55E' : '#6B7280'} 
                />
                <Text style={[
                  styles.statusText,
                  { color: order.status === 'open' ? '#22C55E' : '#6B7280' }
                ]}>
                  {order.status === 'open' ? 'Açık' : 'Kapalı'}
                </Text>
              </View>
            </View>
          </View>
        </LinearGradient>
        <View style={styles.headerBorder} />
      </BlurView>

      <FlatList
        data={[{ isReceipt: true }]}
        renderItem={() => (
          <View style={styles.receiptContainer}>
            <BlurView intensity={40} tint="light" style={styles.receiptBlur}>
              {/* Receipt Paper Effect */}
              <LinearGradient
                colors={['rgba(255, 255, 255, 0.95)', 'rgba(250, 250, 250, 0.98)', 'rgba(255, 255, 255, 0.95)']}
                style={styles.receiptPaper}
              >
                {/* Receipt Header */}
                <View style={styles.receiptHeader}>
                  <Text style={styles.receiptTitle}>ADİSYON</Text>
                  <View style={styles.receiptDivider} />
                  <Text style={styles.receiptDate}>
                    {new Date(order.opened_at).toLocaleDateString('tr-TR')}
                  </Text>
                  <Text style={styles.receiptTime}>
                    {new Date(order.opened_at).toLocaleTimeString('tr-TR', {
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </Text>
                </View>

                {/* Receipt Items */}
                <View style={styles.receiptItems}>
                  {orderItems.map((item, index) => {
                    const statusInfo = getStatusColor(item.status);
                    return (
                      <View key={item.id}>
                        {/* Item Row */}
                        <View style={styles.receiptItemRow}>
                          <View style={styles.receiptItemLeft}>
                            <Text style={styles.receiptItemQty}>{item.quantity}</Text>
                            <Text style={styles.receiptItemName}>{item.menu_item?.name}</Text>
                          </View>
                          <Text style={styles.receiptItemPrice}>
                            ₺{item.total_price.toFixed(2)}
                          </Text>
                        </View>

                        {/* Status & Actions */}
                        <View style={styles.receiptItemStatus}>
                          <View style={[styles.receiptStatusBadge, { backgroundColor: statusInfo.bg }]}>
                            <Ionicons name={statusInfo.iconName} size={14} color={statusInfo.text} />
                            <Text style={[styles.receiptStatusText, { color: statusInfo.text }]}>
                              {getStatusLabel(item.status)}
                            </Text>
                          </View>

                          <View style={styles.receiptStatusActions}>
                            <TouchableOpacity
                              style={[
                                styles.receiptActionBtn,
                                item.status === 'pending' && styles.receiptActionBtnActive,
                              ]}
                              onPress={() => updateItemStatus(item.id, 'pending')}
                              activeOpacity={0.7}
                            >
                              <Ionicons name="time-outline" size={18} color={item.status === 'pending' ? '#F59E0B' : '#6B7280'} />
                            </TouchableOpacity>

                            <TouchableOpacity
                              style={[
                                styles.receiptActionBtn,
                                item.status === 'in_kitchen' && styles.receiptActionBtnActive,
                              ]}
                              onPress={() => updateItemStatus(item.id, 'in_kitchen')}
                              activeOpacity={0.7}
                            >
                              <Ionicons name="restaurant-outline" size={18} color={item.status === 'in_kitchen' ? '#3B82F6' : '#6B7280'} />
                            </TouchableOpacity>

                            <TouchableOpacity
                              style={[
                                styles.receiptActionBtn,
                                item.status === 'served' && styles.receiptActionBtnActive,
                              ]}
                              onPress={() => updateItemStatus(item.id, 'served')}
                              activeOpacity={0.7}
                            >
                              <Ionicons name="checkmark-circle-outline" size={18} color={item.status === 'served' ? '#22C55E' : '#6B7280'} />
                            </TouchableOpacity>
                          </View>
                        </View>

                        {/* Dotted Divider */}
                        {index < orderItems.length - 1 && (
                          <View style={styles.receiptDottedLine} />
                        )}
                      </View>
                    );
                  })}
                </View>

                {/* Receipt Total */}
                <View style={styles.receiptDivider} />
                <View style={styles.receiptTotal}>
                  <Text style={styles.receiptTotalLabel}>TOPLAM</Text>
                  <Text style={styles.receiptTotalAmount}>₺{totalAmount.toFixed(2)}</Text>
                </View>
                <View style={styles.receiptSubtotal}>
                  <Text style={styles.receiptSubtotalText}>{orderItems.length} Ürün</Text>
                </View>

                {/* Receipt Footer */}
                <View style={styles.receiptFooter}>
                  <Ionicons name="restaurant" size={24} color="rgba(26, 26, 46, 0.4)" />
                  <Text style={styles.receiptFooterText}>Afiyet Olsun</Text>
                </View>
              </LinearGradient>

              {/* Paper Shadow/Edges */}
              <View style={styles.receiptShadowTop} />
              <View style={styles.receiptShadowBottom} />
            </BlurView>
          </View>
        )}
        keyExtractor={() => 'receipt'}
        contentContainerStyle={styles.listContainer}
        refreshControl={
          <RefreshControl 
            refreshing={refreshing} 
            onRefresh={onRefresh}
            tintColor="#007AFF"
          />
        }
      />

      {/* Add Item Modal */}
      <Modal
        visible={showAddItemModal}
        animationType="slide"
        transparent={true}
        onRequestClose={() => setShowAddItemModal(false)}
      >
        <View style={styles.modalOverlay}>
          <BlurView intensity={80} tint="dark" style={styles.modalBlur}>
            <View style={styles.modalContainer}>
              <View style={styles.modalHeader}>
                <Text style={styles.modalTitle}>Ürün Ekle</Text>
                <TouchableOpacity
                  onPress={() => setShowAddItemModal(false)}
                  style={styles.modalCloseBtn}
                >
                  <Ionicons name="close-circle" size={32} color="#6B7280" />
                </TouchableOpacity>
              </View>

              {loadingMenu ? (
                <View style={styles.modalLoading}>
                  <ActivityIndicator size="large" color="#3B82F6" />
                </View>
              ) : (
                <FlatList
                  data={menuItems}
                  renderItem={({ item }) => (
                    <TouchableOpacity
                      style={styles.menuItemCard}
                      onPress={() => addItemToOrder(item)}
                      disabled={addingItem}
                      activeOpacity={0.7}
                    >
                      <View style={styles.menuItemInfo}>
                        <Text style={styles.menuItemName}>{item.name}</Text>
                        {item.description && (
                          <Text style={styles.menuItemDesc}>{item.description}</Text>
                        )}
                      </View>
                      <View style={styles.menuItemRight}>
                        <Text style={styles.menuItemPrice}>₺{item.price.toFixed(2)}</Text>
                        <Ionicons name="add-circle" size={28} color="#3B82F6" />
                      </View>
                    </TouchableOpacity>
                  )}
                  keyExtractor={(item) => item.id}
                  contentContainerStyle={styles.menuList}
                  ListEmptyComponent={
                    <Text style={styles.emptyMenuText}>Menüde ürün bulunamadı</Text>
                  }
                />
              )}
            </View>
          </BlurView>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 32,
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: 'rgba(26, 26, 46, 0.6)',
    fontWeight: '500',
  },
  emptyBlur: {
    padding: 32,
    borderRadius: 24,
    backgroundColor: 'rgba(255, 255, 255, 0.12)',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.25)',
  },
  emptyText: {
    fontSize: 16,
    color: 'rgba(26, 26, 46, 0.6)',
    fontWeight: '500',
    textAlign: 'center',
  },
  lightStreak1: {
    position: 'absolute',
    top: -height * 0.2,
    right: -width * 0.3,
    width: width * 1.2,
    height: height * 0.6,
    transform: [{ rotate: '25deg' }],
  },
  lightStreak2: {
    position: 'absolute',
    bottom: -height * 0.2,
    left: -width * 0.3,
    width: width * 1.2,
    height: height * 0.5,
    transform: [{ rotate: '-15deg' }],
  },
  streakGradient: {
    flex: 1,
  },
  headerBlur: {
    marginHorizontal: 16,
    marginTop: 16,
    marginBottom: 12,
    borderRadius: 24,
    overflow: 'hidden',
    backgroundColor: 'rgba(255, 255, 255, 0.12)',
    borderWidth: 1.5,
    borderColor: 'rgba(255, 255, 255, 0.25)',
  },
  headerGradient: {
    padding: 20,
  },
  headerBorder: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    borderRadius: 24,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.3)',
    pointerEvents: 'none',
  },
  headerContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  tableName: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1A1A2E',
    letterSpacing: -0.5,
  },
  orderTime: {
    fontSize: 14,
    fontWeight: '500',
    color: 'rgba(26, 26, 46, 0.6)',
    marginTop: 4,
  },
  headerActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  addButton: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 16,
    gap: 6,
  },
  addButtonText: {
    fontSize: 13,
    fontWeight: '600',
    color: '#3B82F6',
  },
  deleteButton: {
    backgroundColor: 'rgba(239, 68, 68, 0.1)',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 16,
    justifyContent: 'center',
    alignItems: 'center',
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 16,
    gap: 4,
  },
  statusText: {
    fontSize: 13,
    fontWeight: '600',
  },
  listContainer: {
    padding: 16,
    paddingBottom: 32,
  },
  // Receipt Container
  receiptContainer: {
    marginHorizontal: 8,
  },
  receiptBlur: {
    borderRadius: 16,
    overflow: 'visible',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 20 },
    shadowOpacity: 0.25,
    shadowRadius: 30,
    elevation: 20,
  },
  receiptPaper: {
    borderRadius: 16,
    borderWidth: 1,
    borderColor: 'rgba(0, 0, 0, 0.08)',
    overflow: 'hidden',
  },
  receiptShadowTop: {
    position: 'absolute',
    top: -2,
    left: 8,
    right: 8,
    height: 2,
    backgroundColor: 'rgba(0, 0, 0, 0.05)',
    borderRadius: 16,
  },
  receiptShadowBottom: {
    position: 'absolute',
    bottom: -4,
    left: 12,
    right: 12,
    height: 4,
    backgroundColor: 'rgba(0, 0, 0, 0.08)',
    borderRadius: 16,
  },
  // Receipt Header
  receiptHeader: {
    alignItems: 'center',
    paddingTop: 24,
    paddingHorizontal: 24,
    paddingBottom: 16,
  },
  receiptTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#1A1A2E',
    letterSpacing: 2,
    marginBottom: 12,
  },
  receiptDivider: {
    width: '100%',
    height: 2,
    backgroundColor: '#1A1A2E',
    marginVertical: 12,
  },
  receiptDate: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1A1A2E',
    fontFamily: 'monospace',
  },
  receiptTime: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1A1A2E',
    marginTop: 4,
    fontFamily: 'monospace',
  },
  // Receipt Items
  receiptItems: {
    paddingHorizontal: 24,
    paddingVertical: 8,
  },
  receiptItemRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 8,
  },
  receiptItemLeft: {
    flexDirection: 'row',
    flex: 1,
    alignItems: 'flex-start',
  },
  receiptItemQty: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1A1A2E',
    marginRight: 8,
    fontFamily: 'monospace',
    minWidth: 24,
  },
  receiptItemName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1A1A2E',
    flex: 1,
    lineHeight: 22,
  },
  receiptItemPrice: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1A1A2E',
    fontFamily: 'monospace',
    marginLeft: 12,
  },
  receiptItemStatus: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
    marginBottom: 16,
    marginLeft: 32,
  },
  receiptStatusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 8,
    gap: 4,
  },
  receiptStatusText: {
    fontSize: 11,
    fontWeight: '600',
  },
  receiptStatusActions: {
    flexDirection: 'row',
    gap: 6,
  },
  receiptActionBtn: {
    width: 32,
    height: 32,
    borderRadius: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.04)',
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: 'rgba(0, 0, 0, 0.08)',
  },
  receiptActionBtnActive: {
    backgroundColor: 'rgba(59, 130, 246, 0.15)',
    borderColor: 'rgba(59, 130, 246, 0.3)',
    transform: [{ scale: 1.05 }],
  },
  receiptDottedLine: {
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0, 0, 0, 0.15)',
    borderStyle: 'dashed',
    marginVertical: 12,
  },
  // Receipt Total
  receiptTotal: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 24,
    paddingTop: 16,
  },
  receiptTotalLabel: {
    fontSize: 18,
    fontWeight: '700',
    color: '#1A1A2E',
    letterSpacing: 1.5,
  },
  receiptTotalAmount: {
    fontSize: 28,
    fontWeight: '700',
    color: '#1A1A2E',
    fontFamily: 'monospace',
    letterSpacing: -0.5,
  },
  receiptSubtotal: {
    alignItems: 'flex-end',
    paddingHorizontal: 24,
    paddingTop: 4,
    paddingBottom: 16,
  },
  receiptSubtotalText: {
    fontSize: 12,
    fontWeight: '500',
    color: 'rgba(26, 26, 46, 0.6)',
    fontFamily: 'monospace',
  },
  // Receipt Footer
  receiptFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 20,
    borderTopWidth: 1,
    borderTopColor: 'rgba(0, 0, 0, 0.1)',
    marginTop: 8,
    gap: 8,
  },
  receiptFooterText: {
    fontSize: 14,
    fontWeight: '600',
    color: 'rgba(26, 26, 46, 0.6)',
    letterSpacing: 1,
  },
  // Modal Styles
  modalOverlay: {
    flex: 1,
    justifyContent: 'flex-end',
  },
  modalBlur: {
    flex: 1,
    justifyContent: 'flex-end',
  },
  modalContainer: {
    backgroundColor: '#FFFFFF',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    maxHeight: '80%',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: -4 },
    shadowOpacity: 0.2,
    shadowRadius: 12,
    elevation: 20,
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0, 0, 0, 0.08)',
  },
  modalTitle: {
    fontSize: 22,
    fontWeight: '700',
    color: '#1A1A2E',
  },
  modalCloseBtn: {
    padding: 4,
  },
  modalLoading: {
    padding: 60,
    alignItems: 'center',
    justifyContent: 'center',
  },
  menuList: {
    padding: 16,
  },
  menuItemCard: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: '#F9FAFB',
    padding: 16,
    borderRadius: 16,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: 'rgba(0, 0, 0, 0.06)',
  },
  menuItemInfo: {
    flex: 1,
    marginRight: 12,
  },
  menuItemName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1A1A2E',
    marginBottom: 4,
  },
  menuItemDesc: {
    fontSize: 13,
    color: 'rgba(26, 26, 46, 0.6)',
    lineHeight: 18,
  },
  menuItemRight: {
    alignItems: 'flex-end',
    gap: 8,
  },
  menuItemPrice: {
    fontSize: 16,
    fontWeight: '700',
    color: '#3B82F6',
    fontFamily: 'monospace',
  },
  emptyMenuText: {
    textAlign: 'center',
    fontSize: 16,
    color: 'rgba(26, 26, 46, 0.5)',
    padding: 40,
  },
});
