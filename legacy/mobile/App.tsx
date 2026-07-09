import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { StatusBar } from 'expo-status-bar';
import LoginScreen from './screens/LoginScreen';
import HomeScreen from './screens/HomeScreen';
import TablesScreen from './screens/TablesScreen';
import CreateOrderScreen from './screens/CreateOrderScreen';
import OrderDetailScreen from './screens/OrderDetailScreen';
import { RootStackParamList } from './types';

const Stack = createNativeStackNavigator<RootStackParamList>();

export default function App() {
  return (
    <NavigationContainer>
      <Stack.Navigator
        initialRouteName="Login"
        screenOptions={{
          headerStyle: {
            backgroundColor: '#3B82F6',
          },
          headerTintColor: '#fff',
          headerTitleStyle: {
            fontWeight: 'bold',
          },
        }}
      >
        <Stack.Screen 
          name="Login" 
          component={LoginScreen}
          options={{ headerShown: false }}
        />
        <Stack.Screen 
          name="Home" 
          component={HomeScreen}
          options={{ 
            title: 'Ordevo',
            headerLeft: () => null,
          }}
        />
        <Stack.Screen 
          name="Tables" 
          component={TablesScreen}
          options={{ title: 'Masalar' }}
        />
        <Stack.Screen 
          name="CreateOrder" 
          component={CreateOrderScreen}
          options={{ title: 'Yeni Sipariş' }}
        />
        <Stack.Screen 
          name="OrderDetail" 
          component={OrderDetailScreen}
          options={{ title: 'Sipariş Detayı' }}
        />
      </Stack.Navigator>
      <StatusBar style="light" />
    </NavigationContainer>
  );
}
