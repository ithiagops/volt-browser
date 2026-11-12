import unittest,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from policy import *
class Policies(unittest.TestCase):
 def test_move_group(self):
  self.assertEqual(move_block(list(range(6)),[3,4,5],0),[3,4,5,0,1,2])
  self.assertEqual(move_block(list(range(6)),[0,1],6),[2,3,4,5,0,1])
  self.assertEqual(move_block(list(range(6)),[2,3],3),list(range(6)))
  self.assertEqual(move_block(list(range(6)),[1,4],5),[0,2,3,1,4,5])
 def test_all_slots(self):
  for size in range(1,9):
   order=list(range(size))
   for mask in range(1,1<<size):
    chosen=[x for x in order if mask&(1<<x)]
    for slot in range(size+1):
     result=move_block(order,chosen,slot)
     self.assertEqual(sorted(result),order)
     self.assertEqual([x for x in result if x in chosen],chosen)
     at=result.index(chosen[0]);self.assertEqual(result[at:at+len(chosen)],chosen)
 def test_close(self):
  self.assertEqual(survivor([1,2,3,4],{2,3},2),4)
  self.assertEqual(survivor([1,2,3,4],{3,4},4),2)
  self.assertEqual(survivor([1,2,3],{2},1),1)
  self.assertIsNone(survivor([1,2],{1,2},2))
 def test_protection(self):
  self.assertTrue(eligible(False,True,False,False,0,False))
  for args in [(True,True,False,False,0,False),(False,False,False,False,0,False),(False,True,True,False,0,False),(False,True,False,True,0,False),(False,True,False,False,1,False),(False,True,False,False,0,True)]:self.assertFalse(eligible(*args))
 def test_pressure(self):
  self.assertFalse(pressure(False,1_200_000_000));self.assertTrue(pressure(False,1_200_000_001));self.assertTrue(pressure(True,1_100_000_000));self.assertFalse(pressure(True,1_000_000_000))
 def test_address(self):
  self.assertEqual(address(' example.com '),'https://example.com');self.assertEqual(address('http://localhost:123'),'http://localhost:123');self.assertIn('q=ol%C3%A1%20mundo',address('olá mundo'))
 def test_trackers(self):
  self.assertTrue(is_tracker('https://x.doubleclick.net/a'));self.assertFalse(is_tracker('https://doubleclick.net.example.com'));self.assertFalse(is_tracker('https://youtube.com'))
 def test_memory(self):self.assertGreater(process_memory(),0)
if __name__=='__main__':unittest.main()
